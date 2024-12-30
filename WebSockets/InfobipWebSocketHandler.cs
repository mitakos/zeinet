using System.Net.WebSockets;
using System.Text.Json;

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

    public async Task HandleConnection()
    {
        try
        {
            // Start listening for ElevenLabs messages
            var elevenLabsTask = HandleElevenLabsMessages();
            // Start listening for Infobip messages
            var infobipTask = HandleInfobipMessages();

            // Wait for either task to complete (or throw)
            await Task.WhenAny(elevenLabsTask, infobipTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection");
        }
    }

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

    private async Task HandleElevenLabsMessage(Dictionary<string, JsonElement> message)
    {
        if (!message.ContainsKey("type"))
            return;

        var messageType = message["type"].GetString();
        switch (messageType)
        {
            case "conversation_initiation_metadata":
                if (message.ContainsKey("conversation_initiation_metadata_event"))
                {
                    var metadata = message["conversation_initiation_metadata_event"];
                    var conversationId = metadata.GetProperty("conversation_id").GetString();
                    if (conversationId != null)
                    {
                        _conversationId = conversationId;
                        _logger.LogInformation("Conversation initialized with ID: {ConversationId}", _conversationId);
                    }
                }
                break;

            case "audio":
                if (message.ContainsKey("audio_event"))
                {
                    var audioEvent = message["audio_event"];
                    var base64Audio = audioEvent.GetProperty("audio_base_64").GetString();
                    if (base64Audio != null)
                    {
                        var audioBytes = Convert.FromBase64String(base64Audio);
                        await _infobipWebSocket.SendAsync(
                            new ArraySegment<byte>(audioBytes),
                            WebSocketMessageType.Binary,
                            true,
                            _cts.Token);
                    }
                }
                break;

            case "ping":
                if (message.ContainsKey("ping_event"))
                {
                    var pingEvent = message["ping_event"];
                    var eventId = pingEvent.GetProperty("event_id").GetInt32();
                    await SendPong(eventId);
                }
                break;
        }
    }

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