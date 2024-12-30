public class CallEvent
{
    public required string CallId { get; set; }
    public required string Status { get; set; }
    public required string Direction { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required Dictionary<string, string> CustomData { get; set; }
} 