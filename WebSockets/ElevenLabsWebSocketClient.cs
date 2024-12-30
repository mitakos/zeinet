using System.Net.WebSockets;

/// <summary>
/// Client for managing WebSocket connections to ElevenLabs
/// Handles audio streaming and connection lifecycle
/// </summary>
public class ElevenLabsWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly ILogger<ElevenLabsWebSocketClient> _logger;
    private readonly string _conversationId;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new WebSocket client for ElevenLabs
    /// </summary>
    /// <param name="conversationId">ID of the conversation to connect to</param>
    /// <param name="logger">Logger for tracking connection status</param>
    public ElevenLabsWebSocketClient(string conversationId, ILogger<ElevenLabsWebSocketClient> logger)
    {
        _webSocket = new ClientWebSocket();
        _conversationId = conversationId;
        _logger = logger;
    }

    /// <summary>
    /// Establishes WebSocket connection to ElevenLabs
    /// Starts background audio receiving task
    /// </summary>
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

    /// <summary>
    /// Sends audio data to ElevenLabs
    /// </summary>
    /// <param name="audioData">Raw audio data to send</param>
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

    /// <summary>
    /// Background task that continuously receives audio from ElevenLabs
    /// </summary>
    private async Task ReceiveAudioLoop()
    {
        var buffer = new byte[8192]; // 8KB buffer for audio chunks
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

    /// <summary>
    /// Cleans up WebSocket resources
    /// </summary>
    public void Dispose()
    {
        _webSocket.Dispose();
    }
} 