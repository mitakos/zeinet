namespace ZEIage.Models
{
    /// <summary>
    /// Lightweight data transfer object for session information
    /// Used for passing essential session identifiers between components
    /// </summary>
    public class SessionData
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
    }
} 