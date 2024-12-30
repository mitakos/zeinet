using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Controller that manages WebSocket connections between Infobip and ElevenLabs
    /// Handles the bidirectional audio streaming between the services
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ElevenLabsService _elevenLabsService;

        public WebSocketController(
            ILogger<WebSocketController> logger,
            ILoggerFactory loggerFactory,
            ElevenLabsService elevenLabsService)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _elevenLabsService = elevenLabsService;
        }

        /// <summary>
        /// Endpoint that accepts WebSocket connections from Infobip
        /// Creates a bridge between Infobip and ElevenLabs WebSocket connections
        /// </summary>
        /// <param name="callId">The ID of the active call</param>
        [HttpGet("connect/{callId}")]
        public async Task HandleWebSocket(string callId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Accept the WebSocket connection from Infobip
            using var infobipWebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            // Create a WebSocket connection to ElevenLabs
            using var elevenLabsWebSocket = await _elevenLabsService.CreateWebSocketConnectionAsync();
            
            // Create a handler to manage both connections
            var handler = new InfobipWebSocketHandler(
                infobipWebSocket,
                elevenLabsWebSocket,
                _loggerFactory.CreateLogger<InfobipWebSocketHandler>()
            );

            // Start handling the bidirectional communication
            await handler.HandleConnection();
        }
    }
} 