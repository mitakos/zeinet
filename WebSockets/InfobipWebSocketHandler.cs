using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ZEIage.WebSockets
{
    /// <summary>
    /// Handles WebSocket connections between Infobip and ElevenLabs
    /// </summary>
    public class InfobipWebSocketHandler
    {
        private readonly WebSocket _infobipWebSocket;
        private readonly WebSocket _elevenLabsWebSocket;
        private readonly ILogger _logger;
        private const int BufferSize = 4096; // 4KB buffer for audio data

        public InfobipWebSocketHandler(
            WebSocket infobipWebSocket,
            WebSocket elevenLabsWebSocket,
            ILogger logger)
        {
            _infobipWebSocket = infobipWebSocket;
            _elevenLabsWebSocket = elevenLabsWebSocket;
            _logger = logger;
        }

        /// <summary>
        /// Handles the bidirectional WebSocket connection
        /// </summary>
        public async Task HandleConnection()
        {
            try
            {
                // Start processing audio in both directions
                await Task.WhenAll(
                    ProcessInfobipToElevenLabs(),
                    ProcessElevenLabsToInfobip()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
                throw;
            }
        }

        private async Task ProcessInfobipToElevenLabs()
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (_infobipWebSocket.State == WebSocketState.Open && 
                       _elevenLabsWebSocket.State == WebSocketState.Open)
                {
                    var result = await _infobipWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.Count > 0)
                    {
                        await _elevenLabsWebSocket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, result.Count),
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Infobip to ElevenLabs audio");
                throw;
            }
        }

        private async Task ProcessElevenLabsToInfobip()
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (_elevenLabsWebSocket.State == WebSocketState.Open && 
                       _infobipWebSocket.State == WebSocketState.Open)
                {
                    var result = await _elevenLabsWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.Count > 0)
                    {
                        await _infobipWebSocket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, result.Count),
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ElevenLabs to Infobip audio");
                throw;
            }
        }
    }
} 