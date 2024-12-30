using System.Net.WebSockets;
using Microsoft.Extensions.Options;
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
    /// Settings required for ElevenLabs integration
    /// </summary>
    public class ElevenLabsSettings
    {
        public required string BaseUrl { get; set; }  // Base URL for ElevenLabs API
        public required string ApiKey { get; set; }   // API key for authentication
        public required string AgentId { get; set; }  // ID of the AI agent to use
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
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", _settings.ApiKey);
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

                string wsUrl;
                if (_settings.RequireAuthentication)
                {
                    wsUrl = await GetSignedUrlAsync();
                    _logger.LogInformation("Got signed WebSocket URL for authenticated connection");
                }
                else
                {
                    wsUrl = $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={_settings.AgentId}";
                }

                // Validate URL
                if (!Uri.TryCreate(wsUrl, UriKind.Absolute, out _))
                {
                    throw new ElevenLabsApiException(
                        "Invalid WebSocket URL received",
                        0,
                        wsUrl);
                }

                _logger.LogInformation("Using WebSocket URL: {Url}", wsUrl);
                return Guid.NewGuid().ToString();
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
            string agentId = null, 
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
        /// Includes transcript, analysis, and collected variables
        /// </summary>
        /// <param name="conversationId">ID of the conversation to retrieve</param>
        /// <returns>Detailed conversation data</returns>
        /// <exception cref="ArgumentNullException">When conversationId is null or empty</exception>
        /// <exception cref="ElevenLabsApiException">When API request fails</exception>
        public async Task<ElevenLabsConversation> GetConversationDetailsAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            return await SendRequestAsync<ElevenLabsConversation>($"/v1/convai/conversations/{conversationId}");
        }

        /// <summary>
        /// Gets a signed URL for authenticated agent access
        /// Required when RequireAuthentication is enabled
        /// </summary>
        /// <returns>Signed WebSocket URL</returns>
        /// <exception cref="InvalidOperationException">When AgentId is missing</exception>
        /// <exception cref="ElevenLabsApiException">When API request fails</exception>
        private async Task<string> GetSignedUrlAsync()
        {
            if (string.IsNullOrEmpty(_settings.AgentId))
            {
                throw new InvalidOperationException("AgentId is required for authentication");
            }

            var response = await SendRequestAsync<SignedUrlResponse>(
                $"/v1/convai/conversation/get_signed_url?agent_id={_settings.AgentId}");
            
            return response?.SignedUrl ?? throw new ElevenLabsApiException(
                "No signed URL in response",
                0,
                "Empty signed_url field");
        }

        /// <summary>
        /// Sends a request to the ElevenLabs API with error handling
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="endpoint">API endpoint to call</param>
        /// <param name="method">HTTP method (defaults to GET)</param>
        /// <param name="data">Optional request body for POST requests</param>
        /// <returns>Deserialized response data</returns>
        /// <exception cref="ElevenLabsApiException">When request fails or response is invalid</exception>
        private async Task<T> SendRequestAsync<T>(string endpoint, HttpMethod method = null, object data = null)
        {
            try
            {
                method ??= HttpMethod.Get;
                HttpResponseMessage response;

                if (method == HttpMethod.Get)
                {
                    response = await _httpClient.GetAsync(endpoint);
                }
                else
                {
                    var content = data != null ? JsonContent.Create(data) : null;
                    response = await _httpClient.PostAsync(endpoint, content);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
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
                    return await response.Content.ReadFromJsonAsync<T>();
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
    }
} 