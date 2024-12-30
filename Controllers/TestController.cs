using Microsoft.AspNetCore.Mvc;
using ZEIage.Services;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Controller for testing voice call functionality
    /// Provides endpoints for testing call initiation and webhook handling
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly InfobipService _infobipService;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;

        public TestController(
            ILogger<TestController> logger,
            InfobipService infobipService,
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager)
        {
            _logger = logger;
            _infobipService = infobipService;
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Test endpoint for initiating a call with predefined parameters
        /// Uses a test phone number and default conversation variables
        /// </summary>
        [HttpPost("call")]
        public async Task<IActionResult> TestCall([FromBody] TestCallRequest request)
        {
            try
            {
                _logger.LogInformation("Starting test call to {PhoneNumber}", request.PhoneNumber);

                // 1. Initiate the call first to get Infobip's ID
                _logger.LogInformation("Initiating Infobip call");
                var callId = await _infobipService.InitiateCallAsync(request.PhoneNumber);
                
                if (string.IsNullOrEmpty(callId))
                {
                    _logger.LogError("Failed to get call ID from Infobip");
                    return BadRequest("Failed to initiate call");
                }

                // 2. Create session using Infobip's call ID
                var session = _sessionManager.CreateSession(callId, request.PhoneNumber);
                _logger.LogInformation("Created session with ID {SessionId} for call {CallId}", callId, callId);

                // 3. Establish WebSocket connection with initial variables
                var initialVariables = new Dictionary<string, string>
                {
                    { "name", "Test User" },
                    { "phone", request.PhoneNumber }
                };

                var webSocket = await _elevenLabsService.ConnectWebSocket(variables: initialVariables);
                _logger.LogInformation("Established WebSocket connection with ElevenLabs");

                // Store WebSocket in session for later use
                _sessionManager.UpdateSession(callId, s => 
                {
                    s.CallId = callId; // Use Infobip's ID
                    s.State = CallSessionState.Calling;
                    s.CustomData["elevenlabsWebSocket"] = "connected";
                    s.ConversationId = s.CustomData.GetValueOrDefault("elevenlabsWebSocket", string.Empty);
                });

                _logger.LogInformation(
                    "Call initiated successfully. SessionId: {SessionId}, CallId: {CallId}",
                    callId, callId);

                return Ok(new
                {
                    sessionId = callId,
                    callId,
                    message = "Test call initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating test call to {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet("sessions")]
        public IActionResult GetActiveSessions()
        {
            try
            {
                var sessions = _sessionManager.GetAllSessions();
                _logger.LogInformation("Retrieved {Count} active sessions", sessions.Count());
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sessions");
                return StatusCode(500, new { error = "Error retrieving sessions" });
            }
        }

        [HttpPost("hangup/{callId}")]
        public async Task<IActionResult> HangupCall(string callId)
        {
            try
            {
                _logger.LogInformation("Hanging up call {CallId}", callId);
                await _infobipService.DisconnectWebSocket(callId);
                
                var session = _sessionManager.GetSessionByCallId(callId);
                if (session != null)
                {
                    _sessionManager.EndSession(session.SessionId);
                    _logger.LogInformation("Ended session {SessionId} for call {CallId}", session.SessionId, callId);
                }
                
                return Ok(new { message = "Call ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hanging up call {CallId}", callId);
                return StatusCode(500, new { error = "Failed to hang up call", details = ex.Message });
            }
        }
    }

    public class TestCallRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }
} 