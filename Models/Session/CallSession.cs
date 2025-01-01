using System.Collections.Concurrent;
using ZEIage.Models.ElevenLabs;
using ZEIage.Models.Infobip;

namespace ZEIage.Models.Session;

public class CallSession
{
    public string SessionId { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public InfobipCallState State { get; set; } = InfobipCallState.CALL_RECEIVED;
    public Dictionary<string, string> Variables { get; set; } = new();
    public List<ElevenLabsMessage> Transcript { get; set; } = new();
    public ElevenLabsMetadata? Metadata { get; set; }
    public ElevenLabsAnalysis? Analysis { get; set; }
    public ConcurrentDictionary<string, string> CustomData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
} 