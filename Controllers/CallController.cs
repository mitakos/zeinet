using Microsoft.AspNetCore.Mvc;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallController : ControllerBase
    {
        private readonly InfobipService _infobipService;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly ILogger<CallController> _logger;

        public CallController(
            InfobipService infobipService,
            ElevenLabsService elevenLabsService,
            ILogger<CallController> logger)
        {
            _infobipService = infobipService;
            _elevenLabsService = elevenLabsService;
            _logger = logger;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            try
            {
                _logger.LogInformation("Starting call initiation to {PhoneNumber}", request.PhoneNumber);
                
                var sessionId = Guid.NewGuid().ToString();
                _logger.LogInformation("Generated sessionId: {SessionId}", sessionId);
                
                // Initialize ElevenLabs conversation
                _logger.LogInformation("Initializing ElevenLabs conversation with variables: {@Variables}", request.Variables);
                string conversationId;
                try
                {
                    conversationId = await _elevenLabsService.InitializeConversationAsync(request.Variables);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ElevenLabs initialization failed");
                    return StatusCode(500, new { error = "Failed to initialize ElevenLabs", details = ex.Message });
                }
                
                // Initiate call through Infobip
                _logger.LogInformation("Initiating Infobip call");
                string callId;
                try
                {
                    callId = await _infobipService.InitiateCallAsync(request.PhoneNumber, sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Infobip call initiation failed");
                    return StatusCode(500, new { error = "Failed to initiate Infobip call", details = ex.Message });
                }

                return Ok(new { 
                    sessionId, 
                    callId, 
                    conversationId,
                    message = "Call initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error initiating call to {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { error = "Unexpected error", details = ex.Message });
            }
        }
    }

    public class InitiateCallRequest
    {
        public required string PhoneNumber { get; set; }
        public required Dictionary<string, string> Variables { get; set; }
    } 
} 