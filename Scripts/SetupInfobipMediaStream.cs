using System.Net.Http.Headers;
using System.Text.Json;

namespace ZEIage.Scripts;

public class SetupInfobipMediaStream
{
    public static async Task<string> CreateMediaStreamConfig(string baseUrl, string apiKey, string websocketUrl)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri($"https://{baseUrl}");
        client.DefaultRequestHeaders.Add("Authorization", $"App {apiKey}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var config = new
        {
            name = "ZEI Project Media Stream",
            mediaStreamType = "RAW",
            url = websocketUrl,
            audioProperties = new
            {
                encoding = "PCM_LINEAR_16",
                sampleRate = 8000,
                channels = 1,
                packetizationTime = 20
            }
        };

        var response = await client.PostAsJsonAsync("calls/1/media-stream-configs", config);
        var content = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create media stream config: {content}");
        }

        var result = JsonDocument.Parse(content);
        return result.RootElement.GetProperty("id").GetString() ?? 
            throw new Exception("No configuration ID in response");
    }
}

// Usage:
// var configId = await SetupInfobipMediaStream.CreateMediaStreamConfig(
//     "yp1341.api.infobip.com",
//     "your-api-key",
//     "wss://your-websocket-url/api/mediastream"); 