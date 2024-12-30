using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ZEIage.Services;

public class AudioStreamHandler
{
    private readonly ILogger<AudioStreamHandler> _logger;
    private readonly byte[] buffer = new byte[8192]; // 8KB buffer
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
    
    public async Task StartAudioBridgeAsync()
    {
        try
        {
            // Start two tasks for bi-directional audio streaming
            var infobipToElevenLabs = ForwardAudioAsync(_infobipSocket, _elevenLabsSocket, "Infobip -> ElevenLabs");
            var elevenLabsToInfobip = ForwardAudioAsync(_elevenLabsSocket, _infobipSocket, "ElevenLabs -> Infobip");
            
            // Wait for both streams to complete
            await Task.WhenAll(infobipToElevenLabs, elevenLabsToInfobip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio bridge");
            throw;
        }
    }

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
                var result = await source.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    _cts.Token);
                
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    _logger.LogTrace("Forwarding {ByteCount} bytes: {Direction}", 
                        result.Count, direction);
                    
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

    public void Stop()
    {
        _cts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
} 