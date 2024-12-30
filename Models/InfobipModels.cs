namespace ZEIage.Models
{
    public class InfobipCallResponse
    {
        public required string Id { get; set; }
        public required InfobipEndpoint Endpoint { get; set; }
        public required string From { get; set; }
        public required string To { get; set; }
        public required string Direction { get; set; }
        public required string State { get; set; }
        public required InfobipMedia Media { get; set; }
        public required string StartTime { get; set; }
        public required int RingDuration { get; set; }
        public required string CallsConfigurationId { get; set; }
        public required InfobipPlatform Platform { get; set; }
        public Dictionary<string, string>? CustomData { get; set; }
    }

    public class InfobipEndpoint
    {
        public required string Type { get; set; }
        public required string PhoneNumber { get; set; }
    }

    public class InfobipMedia
    {
        public required InfobipAudio Audio { get; set; }
        public required InfobipVideo Video { get; set; }
    }

    public class InfobipAudio
    {
        public required bool Muted { get; set; }
        public required bool UserMuted { get; set; }
        public required bool Deaf { get; set; }
    }

    public class InfobipVideo
    {
        public required bool Camera { get; set; }
        public required bool ScreenShare { get; set; }
    }

    public class InfobipPlatform
    {
        public required string ApplicationId { get; set; }
    }
} 