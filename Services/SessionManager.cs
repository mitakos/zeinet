using Microsoft.Extensions.Logging;
using ZEIage.Models;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages active call sessions and their state
    /// </summary>
    public class SessionManager
    {
        private readonly Dictionary<string, CallSession> _sessions = new();
        private readonly ILogger<SessionManager> _logger;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new call session with initial state
        /// </summary>
        public CallSession CreateSession(string phoneNumber, Dictionary<string, string> variables)
        {
            var session = new CallSession
            {
                SessionId = Guid.NewGuid().ToString(),
                PhoneNumber = phoneNumber,
                State = CallSessionState.Initializing,
                StartTime = DateTime.UtcNow,
                Variables = variables,
                CallId = string.Empty,
                ConversationId = string.Empty
            };

            _sessions[session.SessionId] = session;
            _logger.LogInformation("Created new session {SessionId} for {PhoneNumber}", 
                session.SessionId, phoneNumber);
            
            return session;
        }

        /// <summary>
        /// Updates an existing session's state
        /// </summary>
        public void UpdateSession(string sessionId, Action<CallSession> updateAction)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                updateAction(session);
                _logger.LogInformation("Updated session {SessionId}, new state: {State}", 
                    sessionId, session.State);
            }
        }

        /// <summary>
        /// Retrieves a session by its ID
        /// </summary>
        public CallSession? GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        /// <summary>
        /// Finds a session by its associated call ID
        /// </summary>
        public CallSession? GetSessionByCallId(string callId)
        {
            return _sessions.Values.FirstOrDefault(s => s.CallId == callId);
        }

        /// <summary>
        /// Ends a session and records its duration
        /// </summary>
        public void EndSession(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.State = CallSessionState.Finished;
                session.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Ended session {SessionId}, duration: {Duration}s", 
                    sessionId, (session.EndTime - session.StartTime).Value.TotalSeconds);
                _sessions.Remove(sessionId);
            }
        }
    }
} 