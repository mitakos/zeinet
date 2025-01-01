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
    /// Service responsible for all ElevenLabs AI interactions including:
    /// 1. Conversation initialization and management
    /// 2. WebSocket connections for real-time audio
    /// 3. Variable collection and analysis
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
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            ConfigureHttpClient();
        }

        /// <summary>
        /// Initializes a new conversation with the AI agent
        /// Sets up initial context with provided variables
        /// </summary>
        /// <param name="variables">Custom variables to configure the AI agent</param>
        /// <returns>Conversation ID for subsequent interactions</returns>
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

                // Create conversation with agent and variables
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

                _logger.LogInformation(
                    "Initialized conversation {ConversationId} with variables: {@Variables}", 
                    response.ConversationId, 
                    variables);

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
            try
            {
                if (string.IsNullOrEmpty(_settings.AgentId))
                {
                    throw new InvalidOperationException("AgentId is required for authentication");
                }

                // Create a new request to ensure headers are set correctly
                using var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"/v1/convai/conversation/get_signed_url?agent_id={_settings.AgentId}");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get signed URL. Status: {Status}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    throw new ElevenLabsApiException(
                        $"Failed to get signed URL: {response.StatusCode}",
                        (int)response.StatusCode,
                        responseContent);
                }

                var result = await response.Content.ReadFromJsonAsync<SignedUrlResponse>();
                if (result?.SignedUrl == null)
                {
                    throw new ElevenLabsApiException(
                        "No signed URL in response",
                        (int)response.StatusCode,
                        responseContent);
                }

                _logger.LogInformation("Retrieved signed WebSocket URL for agent {AgentId}", _settings.AgentId);
                return result.SignedUrl;
            }
            catch (Exception ex) when (ex is not ElevenLabsApiException)
            {
                _logger.LogError(ex, "Error getting signed URL");
                throw new ElevenLabsApiException(
                    "Error getting signed URL",
                    0,
                    ex.Message);
            }
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
                using var request = new HttpRequestMessage(method, endpoint);
                
                // Add API key header
                request.Headers.Add("xi-api-key", _settings.ApiKey);

                if (data != null)
                {
                    request.Content = JsonContent.Create(data);
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API error: {Status}, Response: {Response}", 
                        response.StatusCode, responseContent);
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
                    var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
                    if (result == null)
                    {
                        throw new ElevenLabsApiException(
                            "Null response from ElevenLabs API",
                            (int)response.StatusCode,
                            responseContent);
                    }
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON parsing error. Response: {Response}", responseContent);
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
        /// Creates a WebSocket connection for real-time audio streaming
        /// Optionally initializes with variables
        /// </summary>
        /// <param name="conversationId">Optional conversation ID to connect to</param>
        /// <param name="variables">Optional variables to initialize the conversation</param>
        /// <returns>WebSocket connection to ElevenLabs</returns>
        /// <exception cref="Exception">When connection fails</exception>
        public async Task<ClientWebSocket> ConnectWebSocket(
            string? conversationId = null,
            Dictionary<string, string>? variables = null)
        {
            try
            {
                _logger.LogInformation("Using ElevenLabs credentials - AgentId: {AgentId}", _settings.AgentId);

                // 1. Get signed URL first
                var signedUrl = await GetSignedUrlAsync();
                _logger.LogInformation("Got signed WebSocket URL for agent {AgentId}", _settings.AgentId);

                // 2. Create and configure WebSocket
                var ws = new ClientWebSocket();
                
                // No need to set xi-api-key header when using signed URL
                await ws.ConnectAsync(new Uri(signedUrl), CancellationToken.None);
                _logger.LogInformation("Connected to ElevenLabs WebSocket");

                // 3. Wait for conversation_initiation_metadata
                var buffer = new byte[4096];
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var initMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation("Received init message: {Message}", initMessage);

                // 4. Send initial variables if provided
                if (variables != null && variables.Count > 0)
                {
                    // According to docs, we need to send variables in this format
                    var config = new
                    {
                        type = "client_variables",
                        client_variables_event = new
                        {
                            variables = variables
                        }
                    };

                    var configJson = JsonSerializer.Serialize(config);
                    _logger.LogInformation("Sending variables message: {Message}", configJson);
                    
                    var configBytes = System.Text.Encoding.UTF8.GetBytes(configJson);
                    await ws.SendAsync(
                        new ArraySegment<byte>(configBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    _logger.LogInformation("Sent initial variables: {@Variables}", variables);
                }

                return ws;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to ElevenLabs WebSocket");
                throw;
            }
        }

        private void ConfigureHttpClient()
        {
            if (string.IsNullOrEmpty(_settings.BaseUrl))
            {
                throw new ArgumentException("BaseUrl is required");
            }

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                throw new ArgumentException("ApiKey is required");
            }

            if (string.IsNullOrEmpty(_settings.AgentId))
            {
                throw new ArgumentException("AgentId is required");
            }

            _logger.LogInformation("Configuring ElevenLabs client with BaseUrl: {BaseUrl}, AgentId: {AgentId}", 
                _settings.BaseUrl, _settings.AgentId);
            
            // Add API key header globally
            if (!_httpClient.DefaultRequestHeaders.Contains("xi-api-key"))
            {
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", _settings.ApiKey);
            }
        }
    }
} 