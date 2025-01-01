using System.Text.Json.Serialization;

namespace ZEIage.Models.Infobip;

public class InfobipWebhookEvent
{
    [JsonPropertyName("conferenceId")]
    public string? ConferenceId { get; set; }

    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("callsConfigurationId")]
    public string CallsConfigurationId { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public InfobipPlatform Platform { get; set; } = new();

    [JsonPropertyName("bulkId")]
    public string? BulkId { get; set; }

    [JsonPropertyName("dialogId")]
    public string? DialogId { get; set; }

    [JsonPropertyName("properties")]
    public InfobipWebhookProperties Properties { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    public DateTimeOffset GetTimestamp()
    {
        return DateTimeOffset.TryParse(Timestamp, out var result) 
            ? result 
            : DateTimeOffset.UtcNow;
    }
}

public class InfobipPlatform
{
    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;
}

public class InfobipWebhookProperties
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("callLog")]
    public InfobipCallLog? CallLog { get; set; }
}

public class InfobipCallLog
{
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public InfobipEndpoint Endpoint { get; set; } = new();

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("answerTime")]
    public string? AnswerTime { get; set; }

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("errorCode")]
    public InfobipErrorCode? ErrorCode { get; set; }
}

public class InfobipEndpoint
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class InfobipErrorCode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
} 