using System.Net.WebSockets;

public class ElevenLabsWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly ILogger<ElevenLabsWebSocketClient> _logger;
    private readonly string _conversationId;
    private bool _isConnected;

    public ElevenLabsWebSocketClient(string conversationId, ILogger<ElevenLabsWebSocketClient> logger)
    {
        _webSocket = new ClientWebSocket();
        _conversationId = conversationId;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        try
        {
            var uri = new Uri($"wss://api.elevenlabs.io/v1/conversational-ai/websocket?conversation_id={_conversationId}");
            await _webSocket.ConnectAsync(uri, CancellationToken.None);
            _isConnected = true;

            // Start receiving audio in background
            _ = ReceiveAudioLoop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to ElevenLabs WebSocket");
            throw;
        }
    }

    public async Task SendAudioAsync(ArraySegment<byte> audioData)
    {
        if (!_isConnected) throw new InvalidOperationException("WebSocket is not connected");

        try
        {
            await _webSocket.SendAsync(audioData, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to ElevenLabs");
            throw;
        }
    }

    private async Task ReceiveAudioLoop()
    {
        var buffer = new byte[8192];
        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Handle received audio - we'll implement this later
                    // This is where we'll send the audio back to Infobip
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ElevenLabs receive loop");
        }
    }

    public void Dispose()
    {
        _webSocket.Dispose();
    }
} 