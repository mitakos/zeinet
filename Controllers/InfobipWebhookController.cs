using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ZEIage.Models.Infobip;
using ZEIage.Services;

namespace ZEIage.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfobipWebhookController : ControllerBase
{
    private readonly ILogger<InfobipWebhookController> _logger;
    private readonly SessionManager _sessionManager;
    private readonly InfobipService _infobipService;

    public InfobipWebhookController(
        ILogger<InfobipWebhookController> logger,
        SessionManager sessionManager,
        InfobipService infobipService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _infobipService = infobipService;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook([FromBody] JsonDocument rawEvent)
    {
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<InfobipWebhookEvent>(
                rawEvent.RootElement.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (webhookEvent == null)
            {
                _logger.LogError("Failed to parse webhook event");
                return BadRequest("Invalid webhook event");
            }

            _logger.LogInformation("Received webhook event: {Event}", 
                rawEvent.RootElement.ToString());

            switch (webhookEvent.Type)
            {
                case "CALL_ESTABLISHED":
                    var session = _sessionManager.GetSessionByCallId(webhookEvent.CallId);
                    if (session != null)
                    {
                        session.State = InfobipCallState.CALL_ESTABLISHED;
                        _logger.LogInformation("Call {CallId} established", webhookEvent.CallId);

                        // Initialize WebSocket connection for media streaming
                        try
                        {
                            var wsConnection = await _infobipService.ConnectToWebSocket(webhookEvent.CallId, session.SessionId);
                            _logger.LogInformation("WebSocket connection established for call {CallId} at {Url}", 
                                webhookEvent.CallId, wsConnection.WebSocketUrl);
                            
                            session.CustomData["websocketUrl"] = wsConnection.WebSocketUrl;
                            session.CustomData["websocketStatus"] = wsConnection.Status;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to establish WebSocket connection for call {CallId}", webhookEvent.CallId);
                            return StatusCode(500, "Failed to establish media connection");
                        }
                    }
                    break;

                case "CALL_FINISHED":
                case "CALL_FAILED":
                    if (webhookEvent.Properties.CallLog != null)
                    {
                        _logger.LogInformation("Call {CallId} ended with state {State}", 
                            webhookEvent.CallId,
                            webhookEvent.Properties.CallLog.State);
                        
                        if (webhookEvent.Properties.CallLog.ErrorCode != null)
                        {
                            _logger.LogWarning("Call error: {ErrorName} - {ErrorDescription}",
                                webhookEvent.Properties.CallLog.ErrorCode.Name,
                                webhookEvent.Properties.CallLog.ErrorCode.Description);
                        }
                    }
                    _sessionManager.EndSession(webhookEvent.CallId);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, "Internal server error");
        }
    }
} 