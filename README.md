# ZEIage - Voice Call Integration

Integration between Infobip Voice Calls and ElevenLabs Conversational AI.

## Implementation Progress

### ✅ Successfully Implemented

1. **Call Flow**
   - Outbound call initiation via Infobip API
   - Call state tracking through webhooks
   - Session management using Infobip's call ID
   - Error handling and logging

2. **ElevenLabs Integration**
   - WebSocket connection establishment
   - Conversation initialization
   - Initial variables passing
   - Response handling

3. **Session Management**
   - Unified session tracking using Infobip's call ID
   - State transitions based on webhooks
   - Custom data storage
   - Active session monitoring

### 🚧 Pending Media Streaming

1. **Current Blocker**
   - Media streaming needs to be enabled on Infobip's side
   - Call connects but no audio flows
   - WebSocket connection for media not established

2. **Ready for Implementation**
   - Audio bridge code is in place
   - WebSocket handlers are implemented
   - Audio format configuration is set
   - Just waiting for Infobip media streaming activation

### 🔄 Testing Flow (Current)

1. **Initiate Test Call**
```powershell
Invoke-RestMethod -Uri "http://localhost:5133/api/test/call" -Method Post -ContentType "application/json" -Body '{"phoneNumber": "+YOUR_PHONE"}'
```

2. **Expected Response**
```json
{
    "sessionId": "call-id-from-infobip",
    "callId": "same-call-id",
    "message": "Test call initiated successfully"
}
```

3. **Current Behavior**
   - Call is initiated successfully
   - Phone rings and can be answered
   - ElevenLabs WebSocket connects
   - No audio flows (pending media streaming enablement)

### 📝 Configuration Requirements

```json
{
  "InfobipSettings": {
    "BaseUrl": "xxxxx.api.infobip.com",
    "ApiKey": "your-api-key",
    "FromNumber": "your-number",
    "ApplicationId": "your-app-id",
    "CallsConfigurationId": "your-config-id"
  },
  "ElevenLabsSettings": {
    "BaseUrl": "api.elevenlabs.io",
    "ApiKey": "your-api-key",
    "AgentId": "your-agent-id"
  }
}
```

### 🔜 Next Steps

1. **Media Streaming**
   - Await Infobip media streaming activation
   - Test WebSocket connection for media
   - Verify audio bridging functionality

2. **Integration Features**
   - Complete variable collection
   - Implement conversation analysis
   - Add session cleanup
   - Enhance error handling

## Configuration

### Required Settings (appsettings.json)

```json
{
  "InfobipSettings": {
    "BaseUrl": "api.infobip.com",
    "ApiKey": "your-infobip-api-key",
    "PhoneNumber": "+XXX",
    "ApplicationId": "your-app-id",
    "CallsConfigurationId": "your-config-id",
    "WebhookUrl": "https://your-domain.com/api/infobipwebhook/events",
    "MediaStreamingEnabled": true
  },
  "ElevenLabsSettings": {
    "BaseUrl": "api.elevenlabs.io",
    "ApiKey": "your-elevenlabs-api-key",
    "AgentId": "your-agent-id",
    "RequireAuthentication": true,
    "AllowedHosts": [
      "your-domain.com"
    ]
  }
}
```

## Testing Without Media Streaming

You can test the following functionality while waiting for media streaming enablement:

1. **Call Flow Testing**
```powershell
# Initiate a test call
Invoke-RestMethod -Uri "http://localhost:5133/api/test/call" -Method Post -ContentType "application/json" -Body '{"phoneNumber": "+YOUR_PHONE"}'
```

2. **ElevenLabs WebSocket**
```powershell
# Test WebSocket connection
Invoke-RestMethod -Uri "http://localhost:5133/api/test/websocket" -Method Get
```

3. **Session Management**
```json
// Current session structure
{
  "sessionId": "infobip-call-id",
  "callId": "same-as-session",
  "state": "ESTABLISHED",
  "customData": {
    "elevenlabsWebSocket": "connected"
  }
}
```

## Audio Flow (Ready for Testing)

```plaintext
[Infobip Call] <-> [WebSocket] <-> [AudioStreamHandler] <-> [WebSocket] <-> [ElevenLabs]
                    (8kHz mono)      (Audio Bridge)        (8kHz mono)
```

1. **Audio Format**
   - Sample Rate: 8000 Hz
   - Channels: 1 (Mono)
   - Bits: 16-bit PCM
   - Endianness: Little-endian

2. **Bridge Implementation**
   ```csharp
   public class AudioStreamHandler
   {
       // Bidirectional audio streaming
       private async Task ForwardAudioAsync(
           WebSocket source, 
           WebSocket destination, 
           string direction)
       {
           // 8KB buffer for audio chunks
           var buffer = new byte[8192];
           while (source.State == WebSocketState.Open)
           {
               var result = await source.ReceiveAsync(buffer);
               if (result.MessageType == WebSocketMessageType.Binary)
               {
                   await destination.SendAsync(buffer, result.Count);
               }
           }
       }
   }
   ```

### Current Limitations

1. **Media Streaming**
   - Infobip needs to enable media streaming
   - Configuration is ready in code
   - WebSocket handlers implemented
   - Audio bridge tested locally

2. **Error Handling**
   - Basic retry logic implemented
   - Need more robust connection recovery
   - Better error reporting needed
   - Session cleanup improvements required

3. **Testing Coverage**
   - Call flow tested successfully
   - WebSocket connections verified
   - Audio bridge needs live testing
   - More unit tests required

### Development Notes

1. **Key Files**
   - `InfobipService.cs`: Call management
   - `ElevenLabsService.cs`: AI conversation
   - `AudioStreamHandler.cs`: Audio bridging
   - `SessionManager.cs`: State management
   - `WebSocketController.cs`: Connection handling

2. **Configuration Requirements**
   ```json
   {
     "ApplicationUrl": "localhost:5133",
     "UseSecureWebSocket": false,
     "InfobipSettings": {
       "MediaStreamingEnabled": true,
       "AudioSettings": {
         "SampleRate": 8000,
         "Channels": 1,
         "BitsPerSample": 16,
         "Endianness": "LITTLE_ENDIAN"
       }
     }
   }
   ```

3. **Required Environment**
   - .NET 7.0+
   - ngrok for webhook testing
   - PowerShell for testing scripts
   - Valid API credentials

### Debugging Tips

1. **Common Issues**
   - WebSocket connection failures
   - Webhook URL accessibility
   - Audio format mismatches
   - Session state inconsistencies

2. **Logging Locations**
   - Call events: `InfobipWebhookController`
   - Audio flow: `AudioStreamHandler`
   - Session states: `SessionManager`
   - WebSocket events: `WebSocketController`

### Deployment & Monitoring

1. **Prerequisites**
   - Public HTTPS endpoint (for webhooks)
   - Valid SSL certificate
   - Infobip account with:
     - Voice enabled
     - Media streaming enabled
     - Valid phone number
   - ElevenLabs account with:
     - Valid API key
     - Configured AI agent

2. **Environment Setup**
   ```bash
   # Development
   ngrok http 5133  # For webhook testing
   
   # Production
   - Use reverse proxy (nginx/IIS)
   - Configure SSL termination
   - Set up health monitoring
   ```

3. **Health Checks**
   - Webhook endpoint: `/api/infobipwebhook/test`
   - WebSocket status: `/api/test/websocket`
   - Active calls: `/api/test/sessions`
   - Service health: `/health`

4. **Monitoring Points**
   ```plaintext
   [Call Initiation] -> [WebSocket Connection] -> [Audio Bridge] -> [Call Termination]
        |                      |                        |               |
     Success Rate         Connection Rate          Audio Flow      Clean Cleanup
   ```

### Known Issues & Workarounds

1. **WebSocket Connections**
   - Issue: Connection drops after inactivity
   - Workaround: Implement keep-alive mechanism
   - Status: Pending implementation

2. **Session Management**
   - Issue: Orphaned sessions on abnormal termination
   - Workaround: Periodic cleanup job
   - Status: Implemented, needs testing

3. **Audio Streaming**
   - Issue: Media streaming not enabled
   - Workaround: Await Infobip activation
   - Status: Pending vendor action

### Project Roadmap

1. **Phase 1: Core Functionality** ✅
   - Basic call flow
   - Session management
   - WebSocket setup

2. **Phase 2: Media Streaming** 🚧
   - Audio bridge implementation
   - Format conversion
   - Bidirectional flow

3. **Phase 3: Production Readiness**
   - Enhanced error handling
   - Comprehensive logging
   - Performance optimization

4. **Phase 4: Advanced Features**
   - Call recording
   - Analytics integration
   - Advanced error recovery