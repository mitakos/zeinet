using System.Net.WebSockets;
using Microsoft.Extensions.Options;

public class ElevenLabsSettings
{
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public required string AgentId { get; set; }
}

public class ElevenLabsService
{
    private readonly HttpClient _httpClient;
    private readonly ElevenLabsSettings _settings;
    private readonly ILogger<ElevenLabsService> _logger;

    public ElevenLabsService(HttpClient httpClient, IOptions<ElevenLabsSettings> settings, ILogger<ElevenLabsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("xi-api-key", _settings.ApiKey);
    }

    public async Task<string> InitializeConversationAsync(Dictionary<string, string> variables)
    {
        try
        {
            // Get signed URL for WebSocket connection
            var response = await _httpClient.GetAsync($"/v1/convai/conversation/get_signed_url?agent_id={_settings.AgentId}");
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("ElevenLabs signed URL response: {Content}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ElevenLabs API error: {response.StatusCode} - {content}");
            }

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (result == null || !result.ContainsKey("signed_url"))
            {
                throw new Exception($"Invalid response from ElevenLabs: {content}");
            }

            // Store the signed URL for WebSocket connection
            var signedUrl = result["signed_url"];
            _logger.LogInformation("Got signed WebSocket URL: {SignedUrl}", signedUrl);

            // Return a temporary conversation ID - the real one will come from WebSocket
            return Guid.NewGuid().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ElevenLabs conversation");
            throw;
        }
    }

    public async Task<ClientWebSocket> CreateWebSocketConnectionAsync()
    {
        var ws = new ClientWebSocket();
        var wsUrl = $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={_settings.AgentId}";
        
        try
        {
            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
            _logger.LogInformation("WebSocket connected successfully");
            return ws;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect WebSocket");
            throw;
        }
    }
} 