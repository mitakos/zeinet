using ZEIage.Models.ElevenLabs;

namespace ZEIage.Models;

public enum CallSessionState
{
    Created,
    Calling,
    Ringing,
    PreEstablished,
    Established,
    MediaChanged,
    Recording,
    RecordingFailed,
    Rejected,
    Busy,
    NoAnswer,
    Failed,
    Ended
}

public class CallSession
{
    public string SessionId { get; set; } = string.Empty;
    public string? CallId { get; set; }
    public string? ConversationId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public CallSessionState State { get; set; } = CallSessionState.Created;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
    public List<ElevenLabsMessage> Transcript { get; set; } = new();
    public ElevenLabsMetadata Metadata { get; set; } = new();
    public ElevenLabsAnalysis Analysis { get; set; } = new();
    public Dictionary<string, string> CustomData { get; set; } = new();
} 