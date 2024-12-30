using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using ZEIage.Services;

namespace ZEIage.Controllers
{
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

        [HttpGet("connect/{callId}")]
        public async Task HandleWebSocket(string callId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var infobipWebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            using var elevenLabsWebSocket = await _elevenLabsService.CreateWebSocketConnectionAsync();
            
            var handler = new InfobipWebSocketHandler(
                infobipWebSocket,
                elevenLabsWebSocket,
                _loggerFactory.CreateLogger<InfobipWebSocketHandler>()
            );

            await handler.HandleConnection();
        }
    }
} 