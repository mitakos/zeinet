using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ZEIage.Services
{
    /// <summary>
    /// Manages WebSocket connections for active voice calls
    /// Maintains a mapping between call IDs and their WebSocket handlers
    /// </summary>
    public class CallWebSocketManager
    {
        private readonly Dictionary<string, InfobipWebSocketHandler> _connections;
        private readonly ILogger<CallWebSocketManager> _logger;

        /// <summary>
        /// Initializes a new instance of the CallWebSocketManager
        /// </summary>
        /// <param name="logger">Logger for tracking WebSocket operations</param>
        public CallWebSocketManager(ILogger<CallWebSocketManager> logger)
        {
            _connections = new Dictionary<string, InfobipWebSocketHandler>();
            _logger = logger;
        }

        /// <summary>
        /// Adds a new WebSocket connection for a call
        /// </summary>
        /// <param name="callId">Unique identifier of the call</param>
        /// <param name="handler">WebSocket handler for managing the connection</param>
        /// <remarks>If a connection already exists for the call ID, it will be replaced</remarks>
        public void AddConnection(string callId, InfobipWebSocketHandler handler)
        {
            _connections[callId] = handler;
        }

        /// <summary>
        /// Removes a WebSocket connection for a call
        /// </summary>
        /// <param name="callId">Unique identifier of the call to remove</param>
        /// <remarks>If no connection exists for the call ID, this operation does nothing</remarks>
        public void RemoveConnection(string callId)
        {
            if (_connections.ContainsKey(callId))
            {
                _connections.Remove(callId);
            }
        }
    }
} 