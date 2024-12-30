using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ZEIage.Models;

namespace ZEIage.Services;

/// <summary>
/// Handles all interactions with Infobip's Voice API and WebSocket connections.
/// Core responsibilities:
/// 1. Initiating outbound calls
/// 2. Managing WebSocket connections for media streaming
/// 3. Handling call state transitions
/// </summary>
public class InfobipService
{
    private readonly HttpClient _httpClient;
    private readonly InfobipSettings _settings;
    private readonly ILogger<InfobipService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    // Tracks active WebSocket connections by call ID
    private readonly Dictionary<string, ClientWebSocket> _activeConnections = new();

    /// <summary>
    /// Initializes the service with required dependencies and validates configuration
    /// </summary>
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
    /// Initiates an outbound call using Infobip's Voice API
    /// Returns the call ID which is used for all subsequent operations
    /// </summary>
    public async Task<string> InitiateCallAsync(string phoneNumber)
    {
        try
        {
            // Construct call request with required parameters
            var request = new
            {
                endpoint = new 
                { 
                    type = "PHONE",
                    phoneNumber 
                },
                from = _settings.FromNumber,
                callsConfigurationId = _settings.CallsConfigurationId,
                platform = new 
                {
                    applicationId = _settings.ApplicationId
                }
            };

            _logger.LogInformation("Initiating call with request: {@Request}", request);
            var response = await _httpClient.PostAsJsonAsync("/calls/1/calls", request);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Infobip response: {Response}", responseContent);
            
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<InfobipCallResponse>();
            
            if (result == null || string.IsNullOrEmpty(result.Id))
            {
                _logger.LogError("Failed to get valid call ID from Infobip");
                return string.Empty;
            }

            _logger.LogInformation("Call initiated with Infobip ID: {CallId}", result.Id);
            return result.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call to {PhoneNumber}", phoneNumber);
            return string.Empty;
        }
    }

    /// <summary>
    /// Establishes WebSocket connection for media streaming
    /// This is called after receiving CALL_ESTABLISHED webhook
    /// </summary>
    public async Task<WebSocketConnectionResponse> ConnectToWebSocket(string callId, string sessionId)
    {
        try
        {
            // First enable media streaming via API
            var request = new { audioFormat = new AudioStreamConfig() };
            _logger.LogInformation("Enabling media streaming for call {CallId}", callId);
            
            var response = await _httpClient.PostAsJsonAsync(
                $"/calls/1/calls/{callId}/connect", 
                request);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Media stream response: {Response}", responseContent);
            response.EnsureSuccessStatusCode();

            // Create and configure WebSocket connection
            var wsUrl = $"wss://{_settings.BaseUrl}/calls/1/media/{callId}";
            _logger.LogInformation("Connecting to WebSocket at {Url}", wsUrl);

            var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("Authorization", $"App {_settings.ApiKey}");
            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
            
            _logger.LogInformation("WebSocket connected for call {CallId}", callId);
            _activeConnections[callId] = ws;

            return new WebSocketConnectionResponse 
            { 
                Status = "CONNECTED",
                WebSocketUrl = wsUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect WebSocket for call {CallId}", callId);
            throw;
        }
    }

    /// <summary>
    /// Starts the audio bridge between Infobip and ElevenLabs WebSockets
    /// Handles bidirectional audio streaming
    /// </summary>
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
    /// Gracefully closes WebSocket connection and removes from active connections
    /// </summary>
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
    /// Gets an active WebSocket connection by call ID
    /// </summary>
    public ClientWebSocket? GetWebSocket(string callId)
    {
        return _activeConnections.TryGetValue(callId, out var ws) ? ws : null;
    }

    private void ConfigureHttpClient()
    {
        var baseUrl = _settings.BaseUrl?.Trim().ToLower() ?? 
            throw new ArgumentException("BaseUrl is required");
        
        if (!baseUrl.StartsWith("http"))
        {
            baseUrl = $"https://{baseUrl}";
        }
        
        _httpClient.BaseAddress = new Uri(baseUrl);
        
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new ArgumentException("ApiKey is required");
        }
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("App", _settings.ApiKey);
    }
} 