using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages active WebSocket connections for voice calls
    /// Tracks and maintains WebSocket handlers for each active call
    /// </summary>
    public class WebSocketManager
    {
        private readonly Dictionary<string, InfobipWebSocketHandler> _connections;
        private readonly ILogger<WebSocketManager> _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _connections = new Dictionary<string, InfobipWebSocketHandler>();
            _logger = logger;
        }

        /// <summary>
        /// Adds a new WebSocket connection for a call
        /// </summary>
        /// <param name="callId">ID of the call</param>
        /// <param name="handler">WebSocket handler for the call</param>
        public void AddConnection(string callId, InfobipWebSocketHandler handler)
        {
            _logger.LogInformation("Adding WebSocket connection for call {CallId}", callId);
            _connections[callId] = handler;
        }

        /// <summary>
        /// Removes and cleans up a WebSocket connection
        /// Called when a call ends or connection is lost
        /// </summary>
        /// <param name="callId">ID of the call to remove</param>
        public void RemoveConnection(string callId)
        {
            if (_connections.ContainsKey(callId))
            {
                _logger.LogInformation("Removing WebSocket connection for call {CallId}", callId);
                _connections.Remove(callId);
            }
        }

        /// <summary>
        /// Gets the WebSocket handler for a specific call
        /// </summary>
        /// <param name="callId">ID of the call</param>
        /// <returns>WebSocket handler if found, null otherwise</returns>
        public InfobipWebSocketHandler? GetConnection(string callId)
        {
            return _connections.TryGetValue(callId, out var handler) ? handler : null;
        }
    }
} 