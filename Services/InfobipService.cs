using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ZEIage.Models;
using ZEIage.Models.Infobip;

namespace ZEIage.Services;

/// <summary>
/// Handles all interactions with Infobip's Voice API and WebSocket connections.
/// </summary>
/// <remarks>
/// Core responsibilities:
/// 1. Initiating outbound calls
/// 2. Managing WebSocket connections for media streaming
/// 3. Handling call state transitions and media stream configuration
/// </remarks>
public class InfobipService
{
    private readonly HttpClient _httpClient;
    private readonly InfobipSettings _settings;
    private readonly ILogger<InfobipService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    // Tracks active WebSocket connections by call ID
    private readonly Dictionary<string, ClientWebSocket> _activeConnections = new();

    /// <summary>
    /// Initializes the service with required dependencies and validates configuration.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls</param>
    /// <param name="settings">Infobip configuration settings</param>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="loggerFactory">Factory for creating loggers for child components</param>
    /// <exception cref="ArgumentNullException">When any required dependency is null</exception>
    public InfobipService(
        HttpClient httpClient,
        IOptions<InfobipSettings> settings,
        ILogger<InfobipService> logger,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        // Configure HTTP client with base URL and authentication
        ConfigureHttpClient();
    }

    /// <summary>
    /// Initiates an outbound call using Infobip's Voice API.
    /// </summary>
    /// <param name="phoneNumber">Target phone number in E.164 format</param>
    /// <returns>Call ID if successful, empty string if failed</returns>
    /// <remarks>
    /// The call ID is used for all subsequent operations including:
    /// - Tracking call state via webhooks
    /// - Establishing media streams
    /// - Managing WebSocket connections
    /// </remarks>
    public async Task<string> InitiateCallAsync(string phoneNumber)
    {
        _logger.LogInformation("Initiating call to {PhoneNumber}", phoneNumber);

        var request = new
        {
            endpoint = new
            {
                type = "PHONE",
                phoneNumber = phoneNumber
            },
            from = _settings.PhoneNumber,
            connectTimeout = 30,
            maxDuration = 300,
            callsConfigurationId = _settings.CallsConfigurationId,
            platform = new
            {
                applicationId = _settings.ApplicationId
            }
        };

        var content = JsonSerializer.Serialize(request);
        _logger.LogInformation("Request content: {Content}", content);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("calls/1/calls", request);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Infobip response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error response from Infobip: {StatusCode} {Content}", 
                    response.StatusCode, responseContent);
                return string.Empty;
            }

            // Parse response manually to handle DateTime
            var jsonResponse = JsonDocument.Parse(responseContent);
            var root = jsonResponse.RootElement;

            if (root.TryGetProperty("id", out var idElement))
            {
                var callId = idElement.GetString();
                if (!string.IsNullOrEmpty(callId))
                {
                    return callId;
                }
            }

            _logger.LogError("No call ID in response");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating call");
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the media stream configuration from Infobip.
    /// </summary>
    /// <param name="configId">Media stream configuration ID</param>
    /// <returns>True if config exists and is valid, false otherwise</returns>
    /// <remarks>
    /// This is called before attempting to connect a call to the media stream
    /// to ensure the configuration is valid and accessible.
    /// </remarks>
    private async Task<bool> GetMediaStreamConfig(string configId)
    {
        try
        {
            _logger.LogInformation("Getting media stream config {ConfigId}", configId);
            
            var response = await _httpClient.GetAsync($"calls/1/media-stream-configs/{configId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Media stream config response: {Response}", responseContent);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media stream config {ConfigId}", configId);
            return false;
        }
    }

    /// <summary>
    /// Establishes WebSocket connection for media streaming.
    /// </summary>
    /// <param name="callId">Active call ID</param>
    /// <param name="sessionId">Session ID for tracking</param>
    /// <returns>WebSocket connection response with status and URL</returns>
    /// <remarks>
    /// This is called after receiving CALL_ESTABLISHED webhook.
    /// The flow is:
    /// 1. Verify media stream config exists
    /// 2. Connect call to media stream
    /// 3. Return WebSocket URL for audio streaming
    /// </remarks>
    /// <exception cref="InvalidOperationException">When media stream config is missing or invalid</exception>
    public async Task<WebSocketConnectionResponse> ConnectToWebSocket(string callId, string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.MediaStreamConfigId))
            {
                throw new InvalidOperationException("MediaStreamConfigId is required. Please run setup script first.");
            }

            // First get the media stream config
            if (!await GetMediaStreamConfig(_settings.MediaStreamConfigId))
            {
                throw new InvalidOperationException("Failed to get media stream config");
            }

            _logger.LogInformation("Connecting call {CallId} to media stream config {ConfigId}", 
                callId, _settings.MediaStreamConfigId);
            
            var request = new
            {
                mediaStream = new {
                    audioProperties = new {
                        mediaStreamConfigId = _settings.MediaStreamConfigId,
                        replaceMedia = false
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"calls/1/calls/{callId}/start-media-stream", 
                request);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Media stream response: {Response}", responseContent);
            response.EnsureSuccessStatusCode();

            return new WebSocketConnectionResponse 
            { 
                Status = "CONNECTED",
                WebSocketUrl = $"{_settings.PublicBaseUrl}/api/mediastream/{callId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect WebSocket for call {CallId}", callId);
            throw;
        }
    }

    /// <summary>
    /// Starts the audio bridge between Infobip and ElevenLabs WebSockets.
    /// </summary>
    /// <param name="callId">Active call ID</param>
    /// <param name="elevenLabsSocket">Connected ElevenLabs WebSocket</param>
    /// <returns>True if bridge started successfully, false otherwise</returns>
    /// <remarks>
    /// The audio bridge handles:
    /// - Forwarding audio from Infobip to ElevenLabs
    /// - Forwarding responses from ElevenLabs to Infobip
    /// - Converting audio formats if needed
    /// </remarks>
    /// <exception cref="InvalidOperationException">When no active WebSocket exists for call</exception>
    public async Task<bool> StartAudioBridgeAsync(string callId, ClientWebSocket elevenLabsSocket)
    {
        try
        {
            if (!_activeConnections.TryGetValue(callId, out var infobipSocket))
            {
                throw new InvalidOperationException($"No active WebSocket for call {callId}");
            }

            var audioHandler = new AudioStreamHandler(
                infobipSocket,
                elevenLabsSocket,
                _loggerFactory.CreateLogger<AudioStreamHandler>());

            await audioHandler.StartAudioBridgeAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio bridge for call {CallId}", callId);
            return false;
        }
    }

    /// <summary>
    /// Gracefully closes WebSocket connection and removes from active connections.
    /// </summary>
    /// <param name="callId">Call ID to disconnect</param>
    /// <remarks>
    /// Called when:
    /// - Call ends normally
    /// - Error occurs requiring cleanup
    /// - Application is shutting down
    /// </remarks>
    public async Task DisconnectWebSocket(string callId)
    {
        if (_activeConnections.TryGetValue(callId, out var ws))
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Call ended", 
                        CancellationToken.None);
                }
                ws.Dispose();
                _activeConnections.Remove(callId);
                _logger.LogInformation("WebSocket disconnected for call {CallId}", callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting WebSocket for call {CallId}", callId);
            }
        }
    }

    /// <summary>
    /// Gets an active WebSocket connection by call ID.
    /// </summary>
    /// <param name="callId">Call ID to look up</param>
    /// <returns>WebSocket if found, null otherwise</returns>
    /// <remarks>
    /// Used to:
    /// - Check if connection exists
    /// - Get connection for audio bridging
    /// - Verify connection state
    /// </remarks>
    public ClientWebSocket? GetWebSocket(string callId)
    {
        return _activeConnections.TryGetValue(callId, out var ws) ? ws : null;
    }

    /// <summary>
    /// Gets the current status of a call directly from Infobip API.
    /// </summary>
    /// <param name="callId">Call ID to check</param>
    /// <returns>Call response with current state if successful, null if failed</returns>
    /// <remarks>
    /// Used to:
    /// - Verify call state
    /// - Get detailed call information
    /// - Debug issues with calls
    /// </remarks>
    public async Task<InfobipCallResponse?> GetCallStatusAsync(string callId)
    {
        try
        {
            _logger.LogInformation("Getting status for call {CallId}", callId);
            var response = await _httpClient.GetAsync($"/calls/1/calls/{callId}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Call status response: {Response}", responseContent);
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InfobipCallResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for call {CallId}", callId);
            return null;
        }
    }

    /// <summary>
    /// Configures the HTTP client with base URL and authentication.
    /// </summary>
    /// <remarks>
    /// Sets up:
    /// - Base URL for API calls
    /// - Authorization header with API key
    /// - JSON content type headers
    /// </remarks>
    /// <exception cref="ArgumentException">When required settings are missing</exception>
    private void ConfigureHttpClient()
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl))
        {
            throw new ArgumentException("BaseUrl is required");
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new ArgumentException("ApiKey is required");
        }

        // Remove any protocol prefix from BaseUrl if present
        var baseUrl = _settings.BaseUrl.Replace("https://", "").Replace("http://", "");
        _httpClient.BaseAddress = new Uri($"https://{baseUrl}");
        
        // Set required headers according to Infobip docs
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"App {_settings.ApiKey}");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation("Configured Infobip client with BaseUrl: {BaseUrl}", _httpClient.BaseAddress);
    }
} 