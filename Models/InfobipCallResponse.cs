namespace ZEIage.Models;

public class InfobipCallResponse
{
    public string Id { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public InfobipEndpoint Endpoint { get; set; } = new();
    public InfobipMedia? Media { get; set; }
    public string CallsConfigurationId { get; set; } = string.Empty;
    public InfobipPlatform Platform { get; set; } = new();
}

public class InfobipEndpoint
{
    public string Type { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class InfobipMedia
{
    public InfobipAudio? Audio { get; set; }
    public InfobipVideo? Video { get; set; }
}

public class InfobipAudio
{
    public bool Muted { get; set; }
    public bool UserMuted { get; set; }
    public bool Deaf { get; set; }
}

public class InfobipVideo
{
    public bool Camera { get; set; }
    public bool ScreenShare { get; set; }
}

public class InfobipPlatform
{
    public string ApplicationId { get; set; } = string.Empty;
} 