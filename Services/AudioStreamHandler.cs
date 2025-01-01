using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ZEIage.Services;

/// <summary>
/// Handles bidirectional audio streaming between Infobip and ElevenLabs WebSockets.
/// </summary>
/// <remarks>
/// Core responsibilities:
/// 1. Forward raw audio from Infobip to ElevenLabs
/// 2. Forward AI responses from ElevenLabs to Infobip
/// 3. Maintain WebSocket connections and handle errors
/// </remarks>
public class AudioStreamHandler
{
    private readonly ClientWebSocket _elevenLabsSocket;
    private readonly ClientWebSocket _infobipSocket;
    private readonly ILogger<AudioStreamHandler> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new audio stream handler for a specific call.
    /// </summary>
    /// <param name="infobipSocket">Connected WebSocket to Infobip</param>
    /// <param name="elevenLabsSocket">Connected WebSocket to ElevenLabs</param>
    /// <param name="logger">Logger for stream operations</param>
    /// <exception cref="ArgumentNullException">When any socket or logger is null</exception>
    public AudioStreamHandler(
        ClientWebSocket infobipSocket,
        ClientWebSocket elevenLabsSocket,
        ILogger<AudioStreamHandler> logger)
    {
        _infobipSocket = infobipSocket ?? throw new ArgumentNullException(nameof(infobipSocket));
        _elevenLabsSocket = elevenLabsSocket ?? throw new ArgumentNullException(nameof(elevenLabsSocket));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts the bidirectional audio bridge between Infobip and ElevenLabs.
    /// </summary>
    /// <returns>Task that completes when the bridge is stopped</returns>
    /// <remarks>
    /// Creates two tasks:
    /// 1. Forward audio from Infobip to ElevenLabs
    /// 2. Forward responses from ElevenLabs to Infobip
    /// 
    /// The bridge continues until:
    /// - Either WebSocket is closed
    /// - An error occurs
    /// - The cancellation token is triggered
    /// </remarks>
    public async Task StartAudioBridgeAsync()
    {
        try
        {
            // Create tasks for both directions
            var infobipToElevenLabs = ForwardAudioAsync(
                _infobipSocket, 
                _elevenLabsSocket,
                "Infobip -> ElevenLabs");

            var elevenLabsToInfobip = ForwardAudioAsync(
                _elevenLabsSocket,
                _infobipSocket,
                "ElevenLabs -> Infobip");

            // Wait for either task to complete
            await Task.WhenAny(infobipToElevenLabs, elevenLabsToInfobip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio bridge");
            throw;
        }
        finally
        {
            _cancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// Forwards audio data from one WebSocket to another.
    /// </summary>
    /// <param name="source">WebSocket to receive audio from</param>
    /// <param name="destination">WebSocket to send audio to</param>
    /// <param name="direction">Description of audio flow direction for logging</param>
    /// <returns>Task that completes when forwarding stops</returns>
    /// <remarks>
    /// The forwarding continues until:
    /// - Source WebSocket is closed
    /// - Destination WebSocket is closed
    /// - An error occurs
    /// - The cancellation token is triggered
    /// 
    /// Audio format:
    /// - PCM Linear 16-bit
    /// - 8000Hz sample rate
    /// - Single channel (mono)
    /// </remarks>
    private async Task ForwardAudioAsync(
        WebSocket source,
        WebSocket destination,
        string direction)
    {
        var buffer = new byte[8192];
        var token = _cancellationTokenSource.Token;

        try
        {
            while (source.State == WebSocketState.Open &&
                   destination.State == WebSocketState.Open &&
                   !token.IsCancellationRequested)
            {
                var result = await source.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    token);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await destination.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        WebSocketMessageType.Binary,
                        result.EndOfMessage,
                        token);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closing: {Direction}", direction);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error in {Direction}", direction);
            throw;
        }
    }

    /// <summary>
    /// Stops audio streaming and cancels ongoing operations
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
} 