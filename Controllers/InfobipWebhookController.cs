using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZEIage.Models;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfobipWebhookController : ControllerBase
    {
        private readonly ILogger<InfobipWebhookController> _logger;
        private readonly ZEIage.Services.WebSocketManager _webSocketManager;
        private readonly InfobipService _infobipService;

        public InfobipWebhookController(
            ILogger<InfobipWebhookController> logger,
            ZEIage.Services.WebSocketManager webSocketManager,
            InfobipService infobipService)
        {
            _logger = logger;
            _webSocketManager = webSocketManager;
            _infobipService = infobipService;
        }

        [HttpPost("events")]
        public async Task<IActionResult> HandleEvent([FromBody] InfobipCallResponse callEvent)
        {
            try 
            {
                _logger.LogInformation("Received webhook event: {@CallEvent}", callEvent);

                switch (callEvent.State?.ToUpper())
                {
                    case "CALLING":
                        _logger.LogInformation("Call {CallId} is ringing", callEvent.Id);
                        return Ok();

                    case "ESTABLISHED":
                        _logger.LogInformation("Call {CallId} is established, connecting WebSocket", callEvent.Id);
                        await _infobipService.ConnectToWebSocket(callEvent.Id);
                        return Ok();

                    case "FINISHED":
                    case "FAILED":
                        _logger.LogInformation("Call {CallId} ended with state {State}", callEvent.Id, callEvent.State);
                        _webSocketManager.RemoveConnection(callEvent.Id);
                        return Ok();

                    default:
                        _logger.LogWarning("Unknown call state {State} for call {CallId}", callEvent.State, callEvent.Id);
                        return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for call {CallId}", callEvent?.Id);
                // Return 200 even on error to prevent Infobip from retrying
                return Ok();
            }
        }

        // Add a test endpoint
        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Webhook test endpoint called");
            return Ok("Webhook endpoint is working");
        }
    }
} 