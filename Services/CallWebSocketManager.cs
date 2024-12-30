using Microsoft.Extensions.Logging;
using ZEIage.WebSockets;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages WebSocket connections for active calls
    /// </summary>
    public class CallWebSocketManager
    {
        private readonly Dictionary<string, InfobipWebSocketHandler> _connections = new();
        private readonly ILogger<CallWebSocketManager> _logger;

        public CallWebSocketManager(ILogger<CallWebSocketManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a new WebSocket connection for a call
        /// </summary>
        public void AddConnection(string callId, InfobipWebSocketHandler handler)
        {
            _connections[callId] = handler;
            _logger.LogInformation("Added WebSocket connection for call {CallId}", callId);
        }

        /// <summary>
        /// Removes a WebSocket connection
        /// </summary>
        public void RemoveConnection(string callId)
        {
            if (_connections.Remove(callId))
            {
                _logger.LogInformation("Removed WebSocket connection for call {CallId}", callId);
            }
        }

        /// <summary>
        /// Gets a WebSocket connection by call ID
        /// </summary>
        public InfobipWebSocketHandler? GetConnection(string callId)
        {
            return _connections.TryGetValue(callId, out var handler) ? handler : null;
        }
    }
} 