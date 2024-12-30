using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using ZEIage.Services;
using ZEIage.Models;
using ZEIage.WebSockets;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Handles WebSocket connections for voice calls
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly ZEIage.Services.WebSocketManager _webSocketManager;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;

        public WebSocketController(
            ILogger<WebSocketController> logger,
            ZEIage.Services.WebSocketManager webSocketManager,
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager)
        {
            _logger = logger;
            _webSocketManager = webSocketManager;
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Accepts WebSocket connections for voice calls
        /// </summary>
        [HttpGet("{callId}")]
        public async Task HandleWebSocket(string callId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Non-WebSocket request received for call {CallId}", callId);
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var session = _sessionManager.GetSessionByCallId(callId);
            if (session == null)
            {
                _logger.LogWarning("No active session found for call {CallId}", callId);
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            try
            {
                _logger.LogInformation("Accepting WebSocket connection for call {CallId}", callId);
                
                using var infobipWebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                using var elevenLabsWebSocket = await _elevenLabsService.ConnectWebSocket(session.ConversationId);

                var handler = new InfobipWebSocketHandler(
                    infobipWebSocket,
                    elevenLabsWebSocket,
                    _logger);

                _webSocketManager.AddConnection(callId, handler);

                try
                {
                    await handler.HandleConnection();
                }
                finally
                {
                    _webSocketManager.RemoveConnection(callId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection for call {CallId}", callId);
                if (HttpContext.WebSockets.IsWebSocketRequest)
                {
                    var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await ws.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Internal server error",
                        CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Test endpoint to verify WebSocket functionality
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("WebSocket endpoint is working");
        }
    }
} 