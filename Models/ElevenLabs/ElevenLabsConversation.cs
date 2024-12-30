namespace ZEIage.Models.ElevenLabs
{
    /// <summary>
    /// Represents a complete conversation with the ElevenLabs agent
    /// </summary>
    public class ElevenLabsConversation
    {
        public string AgentId { get; set; }
        public string ConversationId { get; set; }
        public long StartTimeUnixSecs { get; set; }
        public int CallDurationSecs { get; set; }
        public int MessageCount { get; set; }
        public string Status { get; set; }  // "processing" or "done"
        public string CallSuccessful { get; set; }  // "success", "failure", "unknown"
        public string AgentName { get; set; }
        public List<TranscriptMessage> Transcript { get; set; } = new();
        public ConversationMetadata Metadata { get; set; }
        public ConversationAnalysis Analysis { get; set; }
    }

    /// <summary>
    /// Represents a single message in the conversation transcript
    /// </summary>
    public class TranscriptMessage
    {
        public string Role { get; set; }  // "user" or "assistant"
        public int TimeInCallSecs { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> CollectedVariables { get; set; }
        public double Confidence { get; set; }
        public List<string> IntentTags { get; set; }
        public Dictionary<string, double> Emotions { get; set; }
    }

    /// <summary>
    /// Contains metadata about the conversation
    /// </summary>
    public class ConversationMetadata
    {
        public long StartTimeUnixSecs { get; set; }
        public int CallDurationSecs { get; set; }
        public string PhoneNumber { get; set; }
        public string Language { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public string CallStatus { get; set; }
    }

    /// <summary>
    /// Contains analysis data about the conversation
    /// </summary>
    public class ConversationAnalysis
    {
        public double OverallSentiment { get; set; }
        public List<string> KeyTopics { get; set; }
        public Dictionary<string, int> IntentCounts { get; set; }
        public Dictionary<string, double> EmotionDistribution { get; set; }
    }
} 