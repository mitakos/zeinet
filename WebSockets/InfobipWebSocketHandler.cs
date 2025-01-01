using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ZEIage.WebSockets
{
    /// <summary>
    /// Handles WebSocket connections between Infobip and ElevenLabs.
    /// </summary>
    /// <remarks>
    /// Core responsibilities:
    /// 1. Accept incoming WebSocket connections from Infobip
    /// 2. Forward audio to/from ElevenLabs WebSocket
    /// 3. Handle WebSocket lifecycle events
    /// </remarks>
    public class InfobipWebSocketHandler
    {
        private readonly WebSocket _infobipWebSocket;
        private readonly WebSocket _elevenLabsWebSocket;
        private readonly ILogger<InfobipWebSocketHandler> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Initializes a new WebSocket handler for a specific call.
        /// </summary>
        /// <param name="infobipWebSocket">WebSocket connection from Infobip</param>
        /// <param name="elevenLabsWebSocket">WebSocket connection to ElevenLabs</param>
        /// <param name="logger">Logger for WebSocket operations</param>
        /// <exception cref="ArgumentNullException">When any WebSocket or logger is null</exception>
        public InfobipWebSocketHandler(
            WebSocket infobipWebSocket,
            WebSocket elevenLabsWebSocket,
            ILogger<InfobipWebSocketHandler> logger)
        {
            _infobipWebSocket = infobipWebSocket ?? throw new ArgumentNullException(nameof(infobipWebSocket));
            _elevenLabsWebSocket = elevenLabsWebSocket ?? throw new ArgumentNullException(nameof(elevenLabsWebSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the bidirectional WebSocket connection.
        /// </summary>
        /// <returns>Task that completes when the connection is closed</returns>
        /// <remarks>
        /// Creates two tasks:
        /// 1. Forward audio from Infobip to ElevenLabs
        /// 2. Forward responses from ElevenLabs to Infobip
        /// 
        /// The connection is maintained until:
        /// - Either WebSocket is closed
        /// - An error occurs
        /// - The cancellation token is triggered
        /// </remarks>
        public async Task HandleConnectionAsync()
        {
            try
            {
                var infobipToElevenLabs = ProcessInfobipToElevenLabsAsync();
                var elevenLabsToInfobip = ProcessElevenLabsToInfobipAsync();

                await Task.WhenAny(infobipToElevenLabs, elevenLabsToInfobip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
                throw;
            }
            finally
            {
                _cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Processes audio data from Infobip to ElevenLabs.
        /// </summary>
        /// <returns>Task that completes when processing stops</returns>
        /// <remarks>
        /// Audio flow:
        /// 1. Receive raw audio from Infobip
        /// 2. Forward to ElevenLabs without modification
        /// 3. Continue until connection closed or error
        /// 
        /// Audio format:
        /// - PCM Linear 16-bit
        /// - 8000Hz sample rate
        /// - Single channel (mono)
        /// </remarks>
        private async Task ProcessInfobipToElevenLabsAsync()
        {
            var buffer = new byte[8192];
            var token = _cancellationTokenSource.Token;
            bool metadataReceived = false;

            while (_infobipWebSocket.State == WebSocketState.Open &&
                   _elevenLabsWebSocket.State == WebSocketState.Open &&
                   !token.IsCancellationRequested)
            {
                var result = await _infobipWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (!metadataReceived)
                {
                    // First message is metadata
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var metadata = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation("Received Infobip metadata: {Metadata}", metadata);
                        metadataReceived = true;
                        continue;
                    }
                }

                // Subsequent messages are raw audio
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await _elevenLabsWebSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        WebSocketMessageType.Binary,
                        result.EndOfMessage,
                        token);
                }
            }
        }

        /// <summary>
        /// Processes audio responses from ElevenLabs to Infobip.
        /// </summary>
        /// <returns>Task that completes when processing stops</returns>
        /// <remarks>
        /// Audio flow:
        /// 1. Receive AI-generated audio from ElevenLabs
        /// 2. Forward to Infobip without modification
        /// 3. Continue until connection closed or error
        /// 
        /// Audio format:
        /// - PCM Linear 16-bit
        /// - 8000Hz sample rate
        /// - Single channel (mono)
        /// </remarks>
        private async Task ProcessElevenLabsToInfobipAsync()
        {
            var buffer = new byte[8192];
            var token = _cancellationTokenSource.Token;

            while (_elevenLabsWebSocket.State == WebSocketState.Open &&
                   _infobipWebSocket.State == WebSocketState.Open &&
                   !token.IsCancellationRequested)
            {
                var result = await _elevenLabsWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                await _infobipWebSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    WebSocketMessageType.Binary,
                    result.EndOfMessage,
                    token);
            }
        }
    }
} 