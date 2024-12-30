using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ZEIage.Services;

/// <summary>
/// Handles bidirectional audio streaming between Infobip and ElevenLabs
/// Core responsibilities:
/// 1. Forward audio from Infobip to ElevenLabs (user speech)
/// 2. Forward audio from ElevenLabs to Infobip (AI responses)
/// 3. Maintain WebSocket connections and handle errors
/// </summary>
public class AudioStreamHandler
{
    private readonly ILogger<AudioStreamHandler> _logger;
    private readonly byte[] buffer = new byte[8192]; // 8KB buffer for optimal audio chunks
    private readonly ClientWebSocket _elevenLabsSocket;
    private readonly ClientWebSocket _infobipSocket;
    private readonly CancellationTokenSource _cts;
    
    public AudioStreamHandler(
        ClientWebSocket infobipSocket, 
        ClientWebSocket elevenLabsSocket,
        ILogger<AudioStreamHandler> logger)
    {
        _infobipSocket = infobipSocket;
        _elevenLabsSocket = elevenLabsSocket;
        _logger = logger;
        _cts = new CancellationTokenSource();
    }
    
    /// <summary>
    /// Starts bidirectional audio streaming between Infobip and ElevenLabs
    /// Creates two parallel tasks for each direction
    /// </summary>
    public async Task StartAudioBridgeAsync()
    {
        try
        {
            // Start two tasks for bi-directional audio streaming
            var infobipToElevenLabs = ForwardAudioAsync(
                _infobipSocket, 
                _elevenLabsSocket, 
                "Infobip -> ElevenLabs");

            var elevenLabsToInfobip = ForwardAudioAsync(
                _elevenLabsSocket, 
                _infobipSocket, 
                "ElevenLabs -> Infobip");
            
            // Wait for both streams to complete
            await Task.WhenAll(infobipToElevenLabs, elevenLabsToInfobip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio bridge");
            throw;
        }
    }

    /// <summary>
    /// Forwards audio data from one WebSocket to another
    /// Handles binary messages containing raw audio data
    /// </summary>
    /// <param name="source">WebSocket to receive audio from</param>
    /// <param name="destination">WebSocket to send audio to</param>
    /// <param name="direction">Direction label for logging</param>
    private async Task ForwardAudioAsync(
        WebSocket source, 
        WebSocket destination, 
        string direction)
    {
        try
        {
            while (source.State == WebSocketState.Open && 
                   destination.State == WebSocketState.Open && 
                   !_cts.Token.IsCancellationRequested)
            {
                // Receive audio chunk from source
                var result = await source.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    _cts.Token);
                
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    _logger.LogTrace("Forwarding {ByteCount} bytes: {Direction}", 
                        result.Count, direction);
                    
                    // Forward to destination
                    await destination.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        WebSocketMessageType.Binary,
                        result.EndOfMessage,
                        _cts.Token);
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
        _cts.Cancel();
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
    }
} 