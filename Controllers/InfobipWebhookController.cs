using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZEIage.Models;
using ZEIage.Services;
using WebSocketManager = ZEIage.Services.WebSocketManager;
using Microsoft.Extensions.Configuration;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Handles webhook events from Infobip for call status updates
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InfobipWebhookController : ControllerBase
    {
        private readonly ILogger<InfobipWebhookController> _logger;
        private readonly WebSocketManager _webSocketManager;
        private readonly InfobipService _infobipService;
        private readonly SessionManager _sessionManager;
        private readonly IConfiguration _configuration;

        public InfobipWebhookController(
            ILogger<InfobipWebhookController> logger,
            WebSocketManager webSocketManager,
            InfobipService infobipService,
            SessionManager sessionManager,
            IConfiguration configuration)
        {
            _logger = logger;
            _webSocketManager = webSocketManager;
            _infobipService = infobipService;
            _sessionManager = sessionManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Handles incoming webhook events from Infobip
        /// Updates call session state and manages WebSocket connections
        /// </summary>
        [HttpPost("events")]
        public async Task<IActionResult> HandleEvent([FromBody] InfobipCallResponse callEvent)
        {
            // Log raw request for debugging
            _logger.LogInformation("Webhook received for call {CallId} with state {State}", 
                callEvent?.Id, callEvent?.State);
            
            try 
            {
                if (callEvent == null || string.IsNullOrEmpty(callEvent.Id))
                {
                    _logger.LogWarning("Invalid webhook event received");
                    return Ok(); // Return OK to prevent retries
                }

                _logger.LogInformation("Processing webhook event: State={State}, CallId={CallId}", 
                    callEvent.State, callEvent.Id);
                
                // Find associated session for this call
                var session = _sessionManager.GetSessionByCallId(callEvent.Id);
                
                if (session == null)
                {
                    _logger.LogWarning("No session found for call {CallId}", callEvent.Id);
                    return Ok();
                }

                // Handle different call states
                switch (callEvent.State?.ToUpper())
                {
                    case "CALLING":
                        // Initial state when call is being placed
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Calling);
                        _logger.LogInformation("Call {CallId} is ringing", callEvent.Id);
                        break;

                    case "CALL_RINGING":
                        // Phone is ringing on recipient's device
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Ringing);
                        _logger.LogInformation("Call {CallId} is ringing on device", callEvent.Id);
                        break;

                    case "CALL_PRE_ESTABLISHED":
                        // Call is about to be established
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.PreEstablished);
                        _logger.LogInformation("Call {CallId} is being established", callEvent.Id);
                        break;

                    case "ESTABLISHED":
                        // Call has been answered
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Established);
                        _logger.LogInformation("Call {CallId} is established", callEvent.Id);
                        
                        // Check if media streaming is enabled in config
                        var mediaStreamingEnabled = _configuration.GetValue<bool>("InfobipSettings:MediaStreamingEnabled", false);
                        if (!mediaStreamingEnabled)
                        {
                            _logger.LogInformation("Media streaming not enabled, skipping WebSocket connection for call {CallId}", callEvent.Id);
                            break;
                        }

                        // Attempt WebSocket connection if media streaming is enabled
                        try
                        {
                            var mediaStreamResponse = await _infobipService.ConnectToWebSocket(callEvent.Id, session.SessionId);
                            _logger.LogInformation(
                                "Media stream connected successfully for call {CallId}. Status: {Status}, WebSocket URL: {Url}", 
                                callEvent.Id, 
                                mediaStreamResponse.Status,
                                mediaStreamResponse.WebSocketUrl);

                            // Store the WebSocket URL in session for reference
                            _sessionManager.UpdateSession(session.SessionId, s => 
                                s.CustomData["mediaStreamUrl"] = mediaStreamResponse.WebSocketUrl);
                        }
                        catch (Exception ex) when (ex.Message.Contains("GENERAL_ERROR"))
                        {
                            _logger.LogWarning(
                                "Media streaming appears to be disabled on Infobip side for call {CallId}. Error: {Error}", 
                                callEvent.Id, 
                                ex.Message);
                            
                            // Track media streaming status in session
                            _sessionManager.UpdateSession(session.SessionId, s => 
                                s.CustomData["mediaStreamingStatus"] = "DISABLED_ON_INFOBIP");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error connecting media stream for call {CallId}", callEvent.Id);
                            _sessionManager.UpdateSession(session.SessionId, s => 
                                s.CustomData["mediaStreamingStatus"] = "CONNECTION_FAILED");
                        }
                        break;

                    case "CALL_MEDIA_CHANGED":
                        // Media state changes (mute, unmute, etc.)
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.MediaChanged);
                        _logger.LogInformation("Call {CallId} media state changed: {@Media}", callEvent.Id, callEvent.Media);
                        break;

                    case "CALL_REJECTED":
                        // Recipient actively rejected the call
                        _logger.LogWarning("Call {CallId} was rejected by recipient", callEvent.Id);
                        await _infobipService.DisconnectWebSocket(callEvent.Id);
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Rejected);
                        _sessionManager.EndSession(session.SessionId);
                        break;

                    case "CALL_BUSY":
                        // Recipient was busy
                        _logger.LogWarning("Recipient {PhoneNumber} was busy", session.PhoneNumber);
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Busy);
                        _sessionManager.EndSession(session.SessionId);
                        break;

                    case "CALL_NO_ANSWER":
                        // Call timed out without answer
                        _logger.LogWarning("No answer from {PhoneNumber}", session.PhoneNumber);
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.NoAnswer);
                        _sessionManager.EndSession(session.SessionId);
                        break;

                    case "CALL_RECORDING_STARTED":
                        // Call recording has started
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Recording);
                        _logger.LogInformation("Recording started for call {CallId}", callEvent.Id);
                        break;

                    case "CALL_RECORDING_FAILED":
                        // Call recording failed to start/continue
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.RecordingFailed);
                        _logger.LogError("Recording failed for call {CallId}", callEvent.Id);
                        break;

                    case "FINISHED":
                        // Call ended normally
                        _logger.LogInformation("Call {CallId} finished normally", callEvent.Id);
                        await _infobipService.DisconnectWebSocket(callEvent.Id);
                        _sessionManager.EndSession(session.SessionId);
                        break;

                    case "FAILED":
                        // Call failed for technical reasons
                        _logger.LogWarning("Call {CallId} failed", callEvent.Id);
                        await _infobipService.DisconnectWebSocket(callEvent.Id);
                        _sessionManager.UpdateSession(session.SessionId, s => s.State = CallSessionState.Failed);
                        _sessionManager.EndSession(session.SessionId);
                        break;

                    default:
                        // Unhandled call states
                        _logger.LogInformation("Received unhandled call state {State} for call {CallId}", 
                            callEvent.State, callEvent.Id);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for call {CallId}", callEvent?.Id);
                return Ok(); // Always return OK to prevent Infobip retries
            }
        }

        /// <summary>
        /// Test endpoint to verify webhook is accessible
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Webhook test endpoint called");
            return Ok("Webhook endpoint is working");
        }
    }
} 