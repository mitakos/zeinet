using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZEIage.Models;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages active call sessions and their state
    /// </summary>
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, CallSession> _sessions = new();
        private readonly ILogger<SessionManager> _logger;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new call session
        /// </summary>
        public CallSession CreateSession(string sessionId, string phoneNumber)
        {
            var session = new CallSession
            {
                SessionId = sessionId,
                PhoneNumber = phoneNumber,
                State = CallSessionState.Created,
                StartTime = DateTime.UtcNow
            };

            if (!_sessions.TryAdd(sessionId, session))
            {
                throw new InvalidOperationException($"Session {sessionId} already exists");
            }

            _logger.LogInformation("Created session {SessionId} for {PhoneNumber}", sessionId, phoneNumber);
            return session;
        }

        /// <summary>
        /// Updates an existing session
        /// </summary>
        public void UpdateSession(string sessionId, Action<CallSession> updateAction)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                updateAction(session);
                _logger.LogDebug("Updated session {SessionId}", sessionId);
            }
            else
            {
                _logger.LogWarning("Session {SessionId} not found for update", sessionId);
            }
        }

        /// <summary>
        /// Gets a session by its ID
        /// </summary>
        public CallSession? GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        /// <summary>
        /// Gets a session by its associated call ID
        /// </summary>
        public CallSession? GetSessionByCallId(string callId)
        {
            return _sessions.Values.FirstOrDefault(s => s.CallId == callId);
        }

        /// <summary>
        /// Gets all sessions
        /// </summary>
        public IEnumerable<CallSession> GetAllSessions()
        {
            return _sessions.Values.ToList();
        }

        /// <summary>
        /// Ends a session and calculates its duration
        /// </summary>
        public void EndSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.EndTime = DateTime.UtcNow;
                session.State = CallSessionState.Ended;
                _logger.LogInformation("Ended session {SessionId}", sessionId);
            }
        }
    }
} 