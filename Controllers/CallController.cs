using Microsoft.AspNetCore.Mvc;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Controller for managing voice calls
    /// Handles call initiation and coordination between Infobip and ElevenLabs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CallController : ControllerBase
    {
        private readonly InfobipService _infobipService;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;
        private readonly ILogger<CallController> _logger;

        /// <summary>
        /// Initializes controller with required services
        /// </summary>
        public CallController(
            InfobipService infobipService,
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager,
            ILogger<CallController> logger)
        {
            _infobipService = infobipService;
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <summary>
        /// Initiates a new voice call
        /// Creates session, initializes ElevenLabs, and starts Infobip call
        /// </summary>
        /// <param name="request">Contains phone number and conversation variables</param>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            try
            {
                _logger.LogInformation("Starting call initiation to {PhoneNumber}", request.PhoneNumber);
                
                // Create session first
                var session = _sessionManager.CreateSession(request.PhoneNumber, request.Variables);
                _logger.LogInformation("Generated sessionId: {SessionId}", session.SessionId);
                
                // Initialize ElevenLabs conversation
                _logger.LogInformation("Initializing ElevenLabs conversation with variables: {@Variables}", request.Variables);
                try
                {
                    var conversationId = await _elevenLabsService.InitializeConversationAsync(request.Variables);
                    _sessionManager.UpdateSession(session.SessionId, s => 
                    {
                        s.ConversationId = conversationId;
                        s.State = "ELEVENLABS_READY";
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ElevenLabs initialization failed");
                    _sessionManager.EndSession(session.SessionId);
                    return StatusCode(500, new { error = "Failed to initialize ElevenLabs", details = ex.Message });
                }
                
                // Initiate call through Infobip
                _logger.LogInformation("Initiating Infobip call");
                try
                {
                    var callId = await _infobipService.InitiateCallAsync(request.PhoneNumber, session.SessionId);
                    _sessionManager.UpdateSession(session.SessionId, s => 
                    {
                        s.CallId = callId;
                        s.State = "CALL_INITIATED";
                    });

                    return Ok(new { 
                        sessionId = session.SessionId, 
                        callId = callId, 
                        conversationId = session.ConversationId,
                        message = "Call initiated successfully"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Infobip call initiation failed");
                    _sessionManager.EndSession(session.SessionId);
                    return StatusCode(500, new { error = "Failed to initiate Infobip call", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error initiating call to {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { error = "Unexpected error", details = ex.Message });
            }
        }

        // Add endpoint to get session status
        [HttpGet("session/{sessionId}")]
        public IActionResult GetSessionStatus(string sessionId)
        {
            var session = _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            return Ok(session);
        }
    }

    public class InitiateCallRequest
    {
        public required string PhoneNumber { get; set; }
        public required Dictionary<string, string> Variables { get; set; }
    } 
} 