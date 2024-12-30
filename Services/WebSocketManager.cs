using Microsoft.Extensions.Logging;
using ZEIage.WebSockets;
using System.Collections.Generic;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages active WebSocket connections for voice calls
    /// </summary>
    public class WebSocketManager
    {
        private readonly Dictionary<string, InfobipWebSocketHandler> _connections = new();
        private readonly ILogger<WebSocketManager> _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a new WebSocket connection
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