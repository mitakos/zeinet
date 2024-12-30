using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ZEIage.Services
{
    public class WebSocketManager
    {
        private readonly Dictionary<string, InfobipWebSocketHandler> _connections;
        private readonly ILogger<WebSocketManager> _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _connections = new Dictionary<string, InfobipWebSocketHandler>();
            _logger = logger;
        }

        public void AddConnection(string callId, InfobipWebSocketHandler handler)
        {
            _connections[callId] = handler;
        }

        public void RemoveConnection(string callId)
        {
            if (_connections.ContainsKey(callId))
            {
                _connections.Remove(callId);
            }
        }
    }
} 