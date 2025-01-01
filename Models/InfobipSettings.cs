namespace ZEIage.Models;

public class InfobipSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string CallsConfigurationId { get; set; } = string.Empty;
    public string MediaStreamConfigId { get; set; } = string.Empty;
    public bool MediaStreamingEnabled { get; set; } = true;
} 