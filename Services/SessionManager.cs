using System.Collections.Concurrent;
using ZEIage.Models.Session;
using ZEIage.Models.Infobip;

namespace ZEIage.Services;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, CallSession> _sessions = new();

    public CallSession CreateSession(string callId, string phoneNumber, Dictionary<string, string>? variables = null)
    {
        var session = new CallSession
        {
            SessionId = callId,
            CallId = callId,
            PhoneNumber = phoneNumber,
            State = InfobipCallState.CALL_RECEIVED,
            Variables = variables ?? new Dictionary<string, string>(),
            CreatedAt = DateTime.UtcNow
        };

        _sessions.TryAdd(session.SessionId, session);
        return session;
    }

    public CallSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public CallSession? GetSessionByCallId(string callId)
    {
        return _sessions.Values.FirstOrDefault(s => s.CallId == callId);
    }

    public void UpdateSession(string sessionId, Action<CallSession> updateAction)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            updateAction(session);
        }
    }

    public void EndSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.State = InfobipCallState.CALL_FINISHED;
            session.EndedAt = DateTime.UtcNow;
            _sessions.TryRemove(sessionId, out _);
        }
    }

    public IEnumerable<CallSession> GetAllSessions()
    {
        return _sessions.Values;
    }
} 