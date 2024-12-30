using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;

namespace ZEIage.Services
{
    /// <summary>
    /// Custom exception for ElevenLabs API errors
    /// Contains detailed information about the API response
    /// </summary>
    public class ElevenLabsApiException : Exception
    {
        /// <summary>
        /// HTTP status code from the API response
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Raw response content from the API
        /// </summary>
        public string ResponseContent { get; }

        /// <summary>
        /// Creates a new ElevenLabs API exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="responseContent">Raw API response</param>
        public ElevenLabsApiException(string message, int statusCode, string responseContent) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }

    /// <summary>
    /// Service that handles all interactions with ElevenLabs API
    /// Manages conversation initialization, data retrieval, and WebSocket connections
    /// </summary>
    public class ElevenLabsService
    {
        private readonly HttpClient _httpClient;
        private readonly ElevenLabsSettings _settings;
        private readonly ILogger<ElevenLabsService> _logger;

        /// <summary>
        /// Initializes the ElevenLabs service with required dependencies
        /// </summary>
        /// <param name="httpClient">HTTP client for API requests</param>
        /// <param name="settings">ElevenLabs configuration settings</param>
        /// <param name="logger">Logger for service operations</param>
        public ElevenLabsService(
            HttpClient httpClient, 
            IOptions<ElevenLabsSettings> settings, 
            ILogger<ElevenLabsService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings), "ElevenLabs settings are not configured");

            // Normalize the base URL
            var baseUrl = _settings.BaseUrl?.Trim().ToLower() ?? throw new ArgumentException("BaseUrl is required", nameof(settings));
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                baseUrl = $"https://{baseUrl}";
            }
            _httpClient.BaseAddress = new Uri(baseUrl);

            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                throw new ArgumentException("ApiKey is required", nameof(settings));
            }
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Initializes a new conversation with ElevenLabs
        /// Handles authentication and WebSocket URL generation
        /// </summary>
        /// <param name="variables">Custom variables for the conversation</param>
        /// <returns>A conversation ID for tracking</returns>
        /// <exception cref="ArgumentNullException">When variables is null</exception>
        /// <exception cref="ElevenLabsApiException">When API request fails</exception>
        public async Task<string> InitializeConversationAsync(Dictionary<string, string> variables)
        {
            try
            {
                if (variables == null)
                {
                    throw new ArgumentNullException(nameof(variables));
                }

                // Make an actual API call to initialize the conversation
                var request = new
                {
                    agent_id = _settings.AgentId,
                    variables = variables
                };

                var response = await SendRequestAsync<ConversationInitResponse>(
                    $"/v1/convai/conversation?agent_id={_settings.AgentId}",
                    HttpMethod.Post,
                    request);

                if (response == null)
                {
                    throw new ElevenLabsApiException(
                        "Failed to initialize conversation",
                        0,
                        "Null response from API");
                }

                return response.ConversationId;
            }
            catch (Exception ex) when (ex is not ElevenLabsApiException)
            {
                _logger.LogError(ex, "Error initializing ElevenLabs conversation");
                throw new ElevenLabsApiException(
                    "Error initializing conversation",
                    0,
                    ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all conversations for the specified agent
        /// </summary>
        /// <param name="agentId">Optional agent ID to filter conversations</param>
        /// <param name="pageSize">Number of conversations to retrieve (1-100)</param>
        /// <returns>List of conversations</returns>
        /// <exception cref="ArgumentOutOfRangeException">When pageSize is invalid</exception>
        /// <exception cref="ElevenLabsApiException">When API request fails</exception>
        public async Task<List<ElevenLabsConversation>> GetConversationsAsync(
            string? agentId = null, 
            int pageSize = 30)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100");
            }

            var url = $"/v1/convai/conversations?page_size={pageSize}";
            if (!string.IsNullOrEmpty(agentId))
            {
                url += $"&agent_id={agentId}";
            }

            var response = await SendRequestAsync<ConversationsResponse>(url);
            return response?.Conversations ?? new List<ElevenLabsConversation>();
        }

        /// <summary>
        /// Gets detailed data for a specific conversation
        /// </summary>
        public async Task<ElevenLabsConversation> GetConversationDetailsAsync(
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            return await SendRequestAsync<ElevenLabsConversation>(
                $"/v1/convai/conversation/{conversationId}",
                HttpMethod.Get,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Gets a signed URL for authenticated agent access
        /// </summary>
        public async Task<string> GetSignedUrlAsync()
        {
            if (string.IsNullOrEmpty(_settings.AgentId))
            {
                throw new InvalidOperationException("AgentId is required for authentication");
            }

            var response = await SendRequestAsync<SignedUrlResponse>(
                $"/v1/convai/conversation/get_signed_url?agent_id={_settings.AgentId}");
            
            if (response?.SignedUrl == null)
            {
                throw new ElevenLabsApiException(
                    "No signed URL in response",
                    0,
                    "Empty signed_url field");
            }

            _logger.LogInformation("Retrieved signed WebSocket URL for agent {AgentId}", _settings.AgentId);
            return response.SignedUrl;
        }

        /// <summary>
        /// Sends a request to the ElevenLabs API with error handling
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="endpoint">API endpoint to call</param>
        /// <param name="method">HTTP method (defaults to GET)</param>
        /// <param name="data">Optional request body for POST requests</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deserialized response data</returns>
        /// <exception cref="ElevenLabsApiException">When request fails or response is invalid</exception>
        private async Task<T> SendRequestAsync<T>(
            string endpoint, 
            HttpMethod? method = null, 
            object? data = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                method ??= HttpMethod.Get;
                HttpResponseMessage response;

                if (method == HttpMethod.Get)
                {
                    response = await _httpClient.GetAsync(endpoint, cancellationToken);
                }
                else
                {
                    var content = data != null ? JsonContent.Create(data) : null;
                    response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new ElevenLabsApiException(
                        $"ElevenLabs API error: {response.StatusCode}",
                        (int)response.StatusCode,
                        responseContent);
                }

                if (string.IsNullOrEmpty(responseContent))
                {
                    throw new ElevenLabsApiException(
                        "Empty response from ElevenLabs API",
                        (int)response.StatusCode,
                        responseContent);
                }

                try
                {
                    return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
                }
                catch (JsonException ex)
                {
                    throw new ElevenLabsApiException(
                        "Invalid JSON response from ElevenLabs API",
                        (int)response.StatusCode,
                        responseContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling ElevenLabs API");
                throw new ElevenLabsApiException(
                    "Network error connecting to ElevenLabs API",
                    0,
                    ex.Message);
            }
            catch (ElevenLabsApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling ElevenLabs API");
                throw new ElevenLabsApiException(
                    "Unexpected error calling ElevenLabs API",
                    0,
                    ex.Message);
            }
        }

        /// <summary>
        /// Creates a WebSocket connection to ElevenLabs for real-time audio streaming
        /// </summary>
        public async Task<WebSocket> ConnectWebSocket(string? conversationId = null, Dictionary<string, string>? variables = null)
        {
            try
            {
                // Create WebSocket client with proper options
                var ws = new ClientWebSocket();
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                ws.Options.SetRequestHeader("xi-api-key", _settings.ApiKey);

                // Build WebSocket URL
                var wsUrl = $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={_settings.AgentId}";
                if (!string.IsNullOrEmpty(conversationId))
                {
                    wsUrl += $"&conversation_id={conversationId}";
                }

                // Connect to WebSocket
                await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                _logger.LogInformation("Connected to ElevenLabs WebSocket");

                // Send initial variables if provided
                if (variables != null && variables.Count > 0)
                {
                    var initMessage = new { type = "conversation_init", variables };
                    var initMessageJson = JsonSerializer.Serialize(initMessage);
                    var initMessageBytes = System.Text.Encoding.UTF8.GetBytes(initMessageJson);
                    
                    await ws.SendAsync(
                        new ArraySegment<byte>(initMessageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    _logger.LogInformation("Sent initial variables to ElevenLabs WebSocket");

                    // Wait for metadata response
                    var buffer = new byte[4096];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var response = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Received WebSocket response: {Response}", response);
                }

                return ws;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to ElevenLabs WebSocket");
                throw;
            }
        }
    }
} 