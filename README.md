# ZEIage - Voice Call Integration

Integration between Infobip Voice Calls and ElevenLabs Conversational AI.

## Current Status

### ✅ Implemented Features

1. **Call Management**
   - Call initiation with Infobip
   - Session state tracking
   - Event handling via webhooks
   - Basic error handling

2. **ElevenLabs Integration**
   - Conversation initialization
   - Variable collection setup
   - WebSocket connection handling
   - Authentication flow

3. **Session Management**
   - Active session tracking
   - State transitions
   - Variable storage
   - Custom data handling

4. **API Endpoints**
   - `/api/call/initiate` - Start new calls
   - `/api/call/session/{sessionId}` - Get session status
   - `/api/conversation` - Manage conversations
   - `/api/infobipwebhook/events` - Handle Infobip events

### ⚠️ Pending Features

1. **Media Streaming** (Awaiting Infobip Enable)
   - Audio streaming between services
   - Real-time transcription
   - Voice response handling

2. **Integration Features**
   - Zoho CRM integration
   - Enhanced variable collection
   - Session cleanup mechanism
   - Health monitoring

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
Invoke-WebRequest -Uri "http://localhost:5133/api/call/initiate" -Method POST -ContentType "application/json" -Body '{"phoneNumber": "+XXXXXXXXXXXXX", "variables": {"name": "Test User"}}'

# Check session status
Invoke-WebRequest -Uri "http://localhost:5133/api/call/session/{sessionId}" -Method GET
```

2. **Conversation Testing**
```powershell
# Get all conversations
Invoke-WebRequest -Uri "http://localhost:5133/api/conversation" -Method GET

# Get conversation details
Invoke-WebRequest -Uri "http://localhost:5133/api/conversation/{conversationId}" -Method GET

# Get conversation transcript
Invoke-WebRequest -Uri "http://localhost:5133/api/conversation/{conversationId}/transcript" -Method GET
```

3. **Webhook Testing**
```powershell
# Test webhook accessibility
Invoke-WebRequest -Uri "http://localhost:5133/api/infobipwebhook/test" -Method GET

# Simulate call events
Invoke-WebRequest -Uri "http://localhost:5133/api/infobipwebhook/events" -Method POST -ContentType "application/json" -Body '{"id":"call-id","state":"ESTABLISHED"}'
```

## Next Steps

1. **Critical Features**
   - Implement initial audio cue mechanism
   - Add WebSocket connection health checks
   - Implement retry logic for failed connections
   - Add session cleanup mechanism

2. **Integration**
   - Complete Zoho CRM integration
   - Enhance variable collection
   - Add monitoring and metrics

3. **Quality**
   - Add comprehensive testing
   - Complete documentation
   - Add health endpoints
   - Implement logging strategy

## Architecture

The application follows a service-oriented architecture with these key components:

1. **Controllers**
   - `CallController` - Manages call initiation and status
   - `ConversationController` - Handles conversation data
   - `InfobipWebhookController` - Processes Infobip events
   - `WebSocketController` - Manages WebSocket connections

2. **Services**
   - `InfobipService` - Handles Infobip API interactions
   - `ElevenLabsService` - Manages ElevenLabs integration
   - `SessionManager` - Tracks active calls
   - `ConversationUpdateService` - Background updates

3. **WebSocket Handlers**
   - `InfobipWebSocketHandler` - Manages Infobip connections
   - `ElevenLabsWebSocketClient` - Handles ElevenLabs streaming

## Development Setup

1. Clone the repository
2. Copy `appsettings.json.template` to `appsettings.json`
3. Fill in your API keys and configuration
4. Run `dotnet build`
5. Start the application with `dotnet run`

## Contributing

1. Create a feature branch
2. Make your changes
3. Add tests if applicable
4. Submit a pull request

## License

This project is proprietary and confidential.