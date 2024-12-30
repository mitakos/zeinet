using System.Text.Json.Serialization;

namespace ZEIage.Models
{
    public class InfobipCallResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("endpoint")]
        public InfobipEndpoint? Endpoint { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = string.Empty;

        [JsonPropertyName("media")]
        public InfobipMedia? Media { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("ringDuration")]
        public int RingDuration { get; set; }

        [JsonPropertyName("callsConfigurationId")]
        public string CallsConfigurationId { get; set; } = string.Empty;

        [JsonPropertyName("platform")]
        public InfobipPlatform Platform { get; set; } = new();

        [JsonPropertyName("customData")]
        public Dictionary<string, string>? CustomData { get; set; }
    }

    public class InfobipEndpoint
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class InfobipMedia
    {
        [JsonPropertyName("audio")]
        public InfobipAudio? Audio { get; set; }

        [JsonPropertyName("video")]
        public InfobipVideo? Video { get; set; }
    }

    public class InfobipAudio
    {
        [JsonPropertyName("muted")]
        public bool Muted { get; set; }

        [JsonPropertyName("userMuted")]
        public bool UserMuted { get; set; }

        [JsonPropertyName("deaf")]
        public bool Deaf { get; set; }
    }

    public class InfobipVideo
    {
        [JsonPropertyName("camera")]
        public bool Camera { get; set; }

        [JsonPropertyName("screenShare")]
        public bool ScreenShare { get; set; }
    }

    public class InfobipPlatform
    {
        [JsonPropertyName("applicationId")]
        public string ApplicationId { get; set; } = string.Empty;
    }
} 