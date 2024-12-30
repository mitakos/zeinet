using System.Net.WebSockets;
using System.Text.Json;

/// <summary>
/// Handles bidirectional WebSocket communication between Infobip and ElevenLabs
/// Manages audio streaming and message processing
/// </summary>
public class InfobipWebSocketHandler : IDisposable
{
    private readonly WebSocket _infobipWebSocket;
    private readonly ClientWebSocket _elevenLabsWebSocket;
    private readonly ILogger<InfobipWebSocketHandler> _logger;
    private readonly byte[] _buffer = new byte[8192]; // 8KB buffer for audio
    private string? _conversationId;
    private bool _isConnected;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public InfobipWebSocketHandler(
        WebSocket infobipWebSocket, 
        ClientWebSocket elevenLabsWebSocket,
        ILogger<InfobipWebSocketHandler> logger)
    {
        _infobipWebSocket = infobipWebSocket;
        _elevenLabsWebSocket = elevenLabsWebSocket;
        _logger = logger;
    }

    /// <summary>
    /// Main method that handles the WebSocket connection lifecycle
    /// Starts parallel tasks for handling messages from both services
    /// </summary>
    public async Task HandleConnection()
    {
        try
        {
            // Start parallel tasks for handling messages
            var elevenLabsTask = HandleElevenLabsMessages();
            var infobipTask = HandleInfobipMessages();

            // Wait for either connection to end
            await Task.WhenAny(elevenLabsTask, infobipTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection");
        }
    }

    /// <summary>
    /// Handles incoming messages from Infobip
    /// Forwards audio data to ElevenLabs
    /// </summary>
    private async Task HandleInfobipMessages()
    {
        try
        {
            while (_infobipWebSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _infobipWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(_buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Forward audio to ElevenLabs
                    if (_elevenLabsWebSocket.State == WebSocketState.Open)
                    {
                        // Convert audio to base64 and wrap in JSON
                        var message = new
                        {
                            user_audio_chunk = Convert.ToBase64String(_buffer, 0, result.Count)
                        };
                        
                        var json = JsonSerializer.Serialize(message);
                        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                        await _elevenLabsWebSocket.SendAsync(
                            new ArraySegment<byte>(bytes), 
                            WebSocketMessageType.Text,
                            true,
                            _cts.Token);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleCloseConnection();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Infobip messages");
            await HandleCloseConnection();
        }
    }

    /// <summary>
    /// Handles incoming messages from ElevenLabs
    /// Processes different message types and forwards audio to Infobip
    /// </summary>
    private async Task HandleElevenLabsMessages()
    {
        var buffer = new byte[8192];
        try
        {
            while (_elevenLabsWebSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _elevenLabsWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Parse and handle JSON messages from ElevenLabs
                    var json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                    if (message != null)
                    {
                        await HandleElevenLabsMessage(message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleCloseConnection();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ElevenLabs messages");
            await HandleCloseConnection();
        }
    }

    /// <summary>
    /// Processes different types of messages from ElevenLabs
    /// Handles conversation metadata and ping/pong messages
    /// </summary>
    private async Task HandleElevenLabsMessage(Dictionary<string, JsonElement> message)
    {
        if (!message.ContainsKey("type"))
            return;

        var messageType = message["type"].GetString();
        switch (messageType)
        {
            case "conversation_initiation_metadata":
                // Handle conversation initialization
                break;

            case "ping":
                // Respond to ping messages to keep connection alive
                if (message.ContainsKey("ping_event"))
                {
                    var pingEvent = message["ping_event"];
                    var eventId = pingEvent.GetProperty("event_id").GetInt32();
                    await SendPong(eventId);
                }
                break;
        }
    }

    /// <summary>
    /// Sends a pong response to ElevenLabs to keep the connection alive
    /// </summary>
    private async Task SendPong(int eventId)
    {
        var pongMessage = new
        {
            type = "pong",
            event_id = eventId
        };

        var json = JsonSerializer.Serialize(pongMessage);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        await _elevenLabsWebSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cts.Token);
    }

    /// <summary>
    /// Gracefully closes both WebSocket connections
    /// </summary>
    private async Task HandleCloseConnection()
    {
        _cts.Cancel();
        
        if (_infobipWebSocket.State == WebSocketState.Open)
            await _infobipWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        
        if (_elevenLabsWebSocket.State == WebSocketState.Open)
            await _elevenLabsWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _elevenLabsWebSocket.Dispose();
    }
} 