# Code Improvements TODO

## 1. Create MediaStreamService
- Extract media stream logic from InfobipService
- Create interface for better testing and separation of concerns
```csharp
public interface IMediaStreamService
{
    Task<bool> ConnectMediaStream(string callId);
    Task<bool> StartAudioBridge(string callId);
    Task DisconnectMediaStream(string callId);
}
```

## 2. Consolidate WebSocket Handling
- Currently duplicated between InfobipWebSocketHandler and AudioStreamHandler
- Create single AudioBridge class:
```csharp
public class AudioBridge 
{
    Task ForwardAudio(WebSocket source, WebSocket destination, string direction);
    Task StartBidirectionalBridge(WebSocket infobip, WebSocket elevenLabs);
}
```

## 3. Better Error Types
- Create specific exceptions instead of using generic ones
- Add context to errors for better debugging
```csharp
public class MediaStreamException : Exception 
{
    public string CallId { get; }
    public MediaStreamErrorType ErrorType { get; }
}

public class WebSocketConnectionException : Exception 
{
    public string ConnectionId { get; }
    public WebSocketState LastState { get; }
}
```

## 4. Centralized Configuration Validation
- Move validation from individual services to central place
- Validate on startup instead of runtime
```csharp
public class ConfigurationValidator
{
    void ValidateInfobipSettings(InfobipSettings settings);
    void ValidateElevenLabsSettings(ElevenLabsSettings settings);
}
```

## 5. Cleanup Tasks
- [ ] Remove duplicate audio forwarding code
- [ ] Add proper IDisposable implementations
- [ ] Standardize error handling patterns
- [ ] Add health checks for WebSocket connections
- [ ] Improve logging consistency across services
