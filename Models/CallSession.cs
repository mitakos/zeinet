namespace ZEIage.Models
{
    /// <summary>
    /// Represents an active voice call session
    /// Contains all information about a call's state and associated data
    /// </summary>
    public class CallSession
    {
        /// <summary>
        /// Unique identifier for the session
        /// </summary>
        public required string SessionId { get; set; }

        /// <summary>
        /// Infobip call identifier
        /// </summary>
        public required string CallId { get; set; }

        /// <summary>
        /// ElevenLabs conversation identifier
        /// </summary>
        public required string ConversationId { get; set; }

        /// <summary>
        /// Phone number of the call recipient
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Current state of the call (see CallSessionState for possible values)
        /// </summary>
        public required string State { get; set; }

        /// <summary>
        /// UTC timestamp when the call started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// UTC timestamp when the call ended, null if call is still active
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Variables passed to ElevenLabs for conversation context
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new();

        /// <summary>
        /// Additional data collected during the call
        /// </summary>
        public Dictionary<string, string> CustomData { get; set; } = new();
    }
} 