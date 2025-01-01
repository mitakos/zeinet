using Microsoft.AspNetCore.Mvc;
using ZEIage.Services;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;
using ZEIage.Models.Infobip;
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
        public async Task<IActionResult> StartCall([FromBody] StartCallRequest request)
        {
            _logger.LogInformation("StartCall called with phoneNumber: {PhoneNumber}", request.PhoneNumber);

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                // Validate phone number
                if (request.PhoneNumber != "+385989821434")
                {
                    _logger.LogError("Invalid phone number. Expected: +385989821434, Got: {PhoneNumber}", request.PhoneNumber);
                    return BadRequest("Invalid phone number. Please use +385989821434 for testing.");
                }

                // Prepare test variables for ElevenLabs
                var initialVariables = new Dictionary<string, string>
                {
                    { "name", "Test User" },
                    { "company", "Test Company" },
                    { "key_insight", "Test Insight" }
                };

                // 1. Connect to ElevenLabs WebSocket with variables
                _logger.LogInformation("Connecting to ElevenLabs WebSocket");
                var webSocket = await _elevenLabsService.ConnectWebSocket(variables: initialVariables);

                if (webSocket.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    _logger.LogError("Failed to connect to ElevenLabs WebSocket");
                    return BadRequest("Failed to connect to ElevenLabs");
                }

                // 2. Now initiate the call with Infobip
                _logger.LogInformation("Initiating Infobip call");
                var callId = await _infobipService.InitiateCallAsync(request.PhoneNumber);

                if (string.IsNullOrEmpty(callId))
                {
                    _logger.LogError("Failed to get call ID from Infobip");
                    return BadRequest("Failed to initiate call");
                }

                // 3. Create session using Infobip's call ID
                var session = _sessionManager.CreateSession(callId, request.PhoneNumber);
                _logger.LogInformation("Created session with ID {SessionId} for call {CallId}", callId, callId);

                // Store WebSocket in session for later use
                _sessionManager.UpdateSession(callId, s =>
                {
                    s.CallId = callId;
                    s.State = InfobipCallState.CALL_ESTABLISHED;
                    s.CustomData["elevenlabsWebSocket"] = "connected";
                    s.Variables = initialVariables;
                });

                return Ok(new { SessionId = session.SessionId, CallId = callId, Message = "Test call initiated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartCall");
                return StatusCode(500, "Internal server error");
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

        [HttpGet("call/{callId}")]
        public async Task<IActionResult> GetCallStatus(string callId)
        {
            var status = await _infobipService.GetCallStatusAsync(callId);
            if (status == null)
            {
                return NotFound($"Call {callId} not found");
            }
            return Ok(status);
        }
    }

    public class TestCallRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class StartCallRequest
    {
        public required string PhoneNumber { get; set; }
    }
} 