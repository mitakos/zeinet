using System.Text.Json.Serialization;

namespace ZEIage.Models.Infobip;

public class InfobipCallResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("establishTime")]
    public DateTime? EstablishTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("media")]
    public InfobipCallMedia? Media { get; set; }
}

public class InfobipCallMedia
{
    [JsonPropertyName("audio")]
    public InfobipAudioState? Audio { get; set; }

    [JsonPropertyName("video")]
    public InfobipVideoState? Video { get; set; }
}

public class InfobipAudioState
{
    [JsonPropertyName("muted")]
    public bool Muted { get; set; }
}

public class InfobipVideoState
{
    [JsonPropertyName("camera")]
    public bool Camera { get; set; }
} 