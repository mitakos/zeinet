using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using ZEIage.Models;

namespace ZEIage.Services
{
    public class InfobipSettings
    {
        public required string BaseUrl { get; set; }
        public required string ApiKey { get; set; }
        public required string PhoneNumber { get; set; }
        public required string ApplicationId { get; set; }
        public required string CallsConfigurationId { get; set; }
        public required string WebhookUrl { get; set; }
    }

    public class InfobipService
    {
        private readonly HttpClient _httpClient;
        private readonly InfobipSettings _settings;
        private readonly ILogger<InfobipService> _logger;
        private readonly string _webSocketBaseUrl;

        public InfobipService(HttpClient httpClient, IOptions<InfobipSettings> settings, ILogger<InfobipService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            
            // Get the application's base URL for WebSocket
            _webSocketBaseUrl = configuration["ApplicationUrl"] ?? "localhost:5133"; // Default for development

            _httpClient.BaseAddress = new Uri($"https://{_settings.BaseUrl}");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"App {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<string> InitiateCallAsync(string phoneNumber, string sessionId)
        {
            try
            {
                var request = new
                {
                    endpoint = new 
                    { 
                        type = "PHONE",
                        phoneNumber = phoneNumber 
                    },
                    from = _settings.PhoneNumber,
                    platform = new
                    {
                        applicationId = _settings.ApplicationId
                    },
                    callsConfigurationId = _settings.CallsConfigurationId,
                    customData = new Dictionary<string, string>
                    {
                        { "sessionId", sessionId }
                    },
                    notifyUrl = _settings.WebhookUrl
                };

                _logger.LogInformation("Initiating call with request: {@Request}", request);

                var response = await _httpClient.PostAsJsonAsync("/calls/1/calls", request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Infobip call response: {Content}", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Infobip API error: {response.StatusCode} - {content}");
                }

                var result = await response.Content.ReadFromJsonAsync<InfobipCallResponse>();
                return result?.Id ?? throw new Exception("Empty response from Infobip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Infobip call");
                throw;
            }
        }

        public async Task ConnectToWebSocket(string callId)
        {
            try
            {
                var wsUrl = $"wss://{_webSocketBaseUrl}/api/websocket/connect/{callId}";
                _logger.LogInformation("Connecting call {CallId} to WebSocket at {Url}", callId, wsUrl);

                var request = new
                {
                    mediaStream = new
                    {
                        type = "websocket",
                        websocket = new
                        {
                            url = wsUrl,
                            audioFormat = new
                            {
                                type = "raw",
                                sampleRate = 8000,
                                channels = 1,
                                bitsPerSample = 16
                            }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync($"/calls/1/calls/{callId}/connect", request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("WebSocket connection response: {Content}", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to connect WebSocket: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting WebSocket for call {CallId}", callId);
                throw;
            }
        }
    }
} 