using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using ZEIage.Services;
using System.Buffers;
using ZEIage.Models;
using ZEIage.WebSockets;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Handles media stream WebSocket connections from Infobip
    /// Manages bidirectional audio streaming between Infobip and ElevenLabs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MediaStreamController : ControllerBase
    {
        private readonly ILogger<MediaStreamController> _logger;
        private readonly ZEIage.Services.WebSocketManager _webSocketManager;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;
        private const int BufferSize = 4096; // 4KB buffer for audio data
        private const int MaxRetries = 3;

        public MediaStreamController(
            ILogger<MediaStreamController> logger,
            ZEIage.Services.WebSocketManager webSocketManager,
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager)
        {
            _logger = logger;
            _webSocketManager = webSocketManager;
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Endpoint that accepts WebSocket connections from Infobip for media streaming
        /// </summary>
        [HttpGet("{callId}")]
        public async Task HandleMediaStream(string callId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Non-WebSocket request received for call {CallId}", callId);
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var session = _sessionManager.GetSessionByCallId(callId);
            if (session == null)
            {
                _logger.LogWarning("No active session found for call {CallId}", callId);
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            try
            {
                _logger.LogInformation("Accepting WebSocket connection for call {CallId}", callId);
                
                using var infobipWebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                using var elevenLabsWebSocket = await ConnectToElevenLabsWithRetry(session.ConversationId);
                
                // Create handler for managing the connections
                var handler = new InfobipWebSocketHandler(
                    infobipWebSocket,
                    elevenLabsWebSocket,
                    _logger);

                // Add the connection to our manager
                _webSocketManager.AddConnection(callId, handler);

                try
                {
                    // Start processing audio in both directions
                    await Task.WhenAll(
                        ProcessInfobipToElevenLabs(infobipWebSocket, elevenLabsWebSocket, callId),
                        ProcessElevenLabsToInfobip(elevenLabsWebSocket, infobipWebSocket, callId)
                    );
                }
                finally
                {
                    _webSocketManager.RemoveConnection(callId);
                    await CloseWebSocketsGracefully(infobipWebSocket, elevenLabsWebSocket);
                }
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "WebSocket error for call {CallId}", callId);
                await HandleWebSocketError(callId, wsEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling media stream for call {CallId}", callId);
                await HandleGeneralError(callId);
            }
        }

        private async Task<WebSocket> ConnectToElevenLabsWithRetry(string conversationId)
        {
            var retryCount = 0;
            var maxRetries = 3;
            var delay = TimeSpan.FromSeconds(1);

            while (retryCount < maxRetries)
            {
                try
                {
                    return await _elevenLabsService.ConnectWebSocket(conversationId);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount == maxRetries)
                        throw;

                    _logger.LogWarning(ex, "Retry {Count} connecting to ElevenLabs. Waiting {Delay}s", 
                        retryCount, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            throw new InvalidOperationException("Failed to connect to ElevenLabs after retries");
        }

        private async Task ProcessInfobipToElevenLabs(WebSocket infobipWs, WebSocket elevenLabsWs, string callId)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (infobipWs.State == WebSocketState.Open && elevenLabsWs.State == WebSocketState.Open)
                {
                    var result = await infobipWs.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.Count > 0)
                    {
                        await elevenLabsWs.SendAsync(
                            new ArraySegment<byte>(buffer, 0, result.Count),
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task ProcessElevenLabsToInfobip(WebSocket elevenLabsWs, WebSocket infobipWs, string callId)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (elevenLabsWs.State == WebSocketState.Open && infobipWs.State == WebSocketState.Open)
                {
                    var result = await elevenLabsWs.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.Count > 0)
                    {
                        await infobipWs.SendAsync(
                            new ArraySegment<byte>(buffer, 0, result.Count),
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task CloseWebSocketsGracefully(params WebSocket[] webSockets)
        {
            foreach (var ws in webSockets)
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing connection",
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket connection");
                }
            }
        }

        private async Task HandleWebSocketError(string callId, WebSocketException ex)
        {
            _sessionManager.UpdateSession(callId, s => s.State = CallSessionState.Failed);
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ws.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    "WebSocket error occurred",
                    CancellationToken.None);
            }
        }

        private async Task HandleGeneralError(string callId)
        {
            _sessionManager.UpdateSession(callId, s => s.State = CallSessionState.Failed);
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ws.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    "Internal server error",
                    CancellationToken.None);
            }
        }

        /// <summary>
        /// Test endpoint to verify media stream endpoint is accessible
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Media stream endpoint is working");
        }
    }
} 