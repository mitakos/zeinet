using Microsoft.AspNetCore.Mvc;
using ZEIage.Models;
using ZEIage.Services;

namespace ZEIage.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallController : ControllerBase
{
    private readonly ILogger<CallController> _logger;
    private readonly InfobipService _infobipService;
    private readonly SessionManager _sessionManager;
    private readonly ElevenLabsService _elevenLabsService;

    public CallController(
        ILogger<CallController> logger,
        InfobipService infobipService,
        SessionManager sessionManager,
        ElevenLabsService elevenLabsService)
    {
        _logger = logger;
        _infobipService = infobipService;
        _sessionManager = sessionManager;
        _elevenLabsService = elevenLabsService;
    }

    [HttpPost]
    public async Task<IActionResult> InitiateCall([FromBody] CallRequest request)
    {
        try
        {
            // Initiate the call with Infobip
            var callId = await _infobipService.InitiateCallAsync(request.PhoneNumber);
            if (string.IsNullOrEmpty(callId))
            {
                return BadRequest("Failed to initiate call");
            }

            // Create a session for the call
            var session = _sessionManager.CreateSession(callId, request.PhoneNumber);

            // Initialize conversation with ElevenLabs
            var conversationId = await _elevenLabsService.InitializeConversationAsync(request.Variables);
            if (string.IsNullOrEmpty(conversationId))
            {
                return BadRequest("Failed to initialize ElevenLabs conversation");
            }

            session.ConversationId = conversationId;
            session.Variables = request.Variables;

            return Ok(new { sessionId = session.SessionId, callId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating call");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{sessionId}/status")]
    public IActionResult GetSessionStatus(string sessionId)
    {
        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(new { status = session.State.ToString() });
    }
}

public class CallRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
} 