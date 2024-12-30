using System.Text.Json.Serialization;

namespace ZEIage.Models.ElevenLabs;

public class ElevenLabsMessage
{
    public string Role { get; set; } = string.Empty;
    public double TimeInCallSecs { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> CollectedVariables { get; set; } = new();
    public List<string> IntentTags { get; set; } = new();
    public Dictionary<string, double> Emotions { get; set; } = new();
}

public class ElevenLabsMetadata
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public Dictionary<string, string> CustomData { get; set; } = new();
    public string CallStatus { get; set; } = string.Empty;
}

public class ElevenLabsAnalysis
{
    public List<string> KeyTopics { get; set; } = new();
    public Dictionary<string, int> IntentCounts { get; set; } = new();
    public Dictionary<string, double> EmotionDistribution { get; set; } = new();
}

public class ElevenLabsSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}

public class ElevenLabsConversation
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ElevenLabsMessage> Messages { get; set; } = new();

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; } = new();

    [JsonPropertyName("metadata")]
    public ElevenLabsMetadata Metadata { get; set; } = new();

    [JsonPropertyName("analysis")]
    public ElevenLabsAnalysis Analysis { get; set; } = new();
}

public class ConversationInitResponse
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;
}

public class ConversationsResponse
{
    [JsonPropertyName("conversations")]
    public List<ElevenLabsConversation> Conversations { get; set; } = new();
}

public class SignedUrlResponse
{
    [JsonPropertyName("signed_url")]
    public string SignedUrl { get; set; } = string.Empty;
} 