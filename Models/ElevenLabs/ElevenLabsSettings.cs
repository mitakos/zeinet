namespace ZEIage.Models.ElevenLabs
{
    /// <summary>
    /// Configuration settings for ElevenLabs integration including security settings
    /// </summary>
    public class ElevenLabsSettings
    {
        public required string BaseUrl { get; set; }
        public required string ApiKey { get; set; }
        public required string AgentId { get; set; }
        public required bool RequireAuthentication { get; set; }  // From Security tab
        public List<string> AllowedHosts { get; set; } = new();  // From Security tab allowlist
    }
} 