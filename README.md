# ZEI Voice Agent Project

A .NET application integrating Infobip for voice calls and ElevenLabs for conversational AI.

## Project Overview

This project implements a voice agent system that:
- Initiates outbound calls via Infobip
- Handles real-time audio streaming
- Processes conversations using ElevenLabs AI
- Manages WebSocket connections for audio streaming

## Project Structure 
ZEIage/
├── Controllers/
│ ├── CallController.cs # Handles call initiation
│ ├── InfobipWebhookController.cs # Handles Infobip webhooks
│ ├── TestController.cs # Test endpoints
│ └── WebSocketController.cs # WebSocket handling
├── Models/
│ └── InfobipModels.cs # Infobip response models
├── Services/
│ ├── InfobipService.cs # Infobip integration
│ ├── ElevenLabsService.cs # ElevenLabs integration
│ └── WebSocketManager.cs # WebSocket connection management
└── appsettings.json # Configuration

## Prerequisites

- .NET 8.0 SDK
- ngrok for webhook testing
- Infobip account with:
  - Voice enabled
  - Media streaming enabled (contact Infobip support)
- ElevenLabs account with Conversational AI access

## Configuration

Update `appsettings.json` with your credentials:
json
{
"ApplicationUrl": "your-ngrok-url",
"InfobipSettings": {
"BaseUrl": "your-infobip-base-url",
"ApiKey": "your-infobip-api-key",
"PhoneNumber": "your-infobip-phone",
"ApplicationId": "your-app-id",
"CallsConfigurationId": "your-calls-config-id",
"WebhookUrl": "your-webhook-url"
},
"ElevenLabsSettings": {
"ApiKey": "your-elevenlabs-api-key",
"BaseUrl": "https://api.elevenlabs.io",
"AgentId": "your-agent-id"
}


## Current Implementation Status

### Working Features ✅
1. **Call Initiation**
   - Successful outbound calls through Infobip
   - Webhook URL configuration
   - Call state management

2. **ElevenLabs Integration**
   - Signed WebSocket URL generation
   - Conversation initialization

3. **Webhook Handling**
   - Infobip webhook processing
   - Call state management
   - Event logging

### Pending Features ❌
1. **Media Streaming**
   - Requires Infobip media streaming enablement
   - WebSocket connection implementation
   - Audio stream handling

2. **Conversation Management**
   - Full conversation flow
   - Audio streaming between services
   - Call termination handling

## Testing

### Prerequisites
1. Run the application:
dotnet run
2. Start ngrok:

3. Update `appsettings.json` with the new ngrok URL

### Test Commands

1. Test webhook endpoint:
powershell
Invoke-WebRequest -Uri "https://your-ngrok-url/api/infobipwebhook/test" -Method GET


## Next Steps

1. **Media Streaming Setup**
   - Contact Infobip to enable media streaming
   - Get media streaming configuration
   - Update application settings

2. **WebSocket Implementation**
   - Update WebSocket URL format
   - Implement audio streaming
   - Add error handling

3. **Conversation Flow**
   - Implement conversation management
   - Add audio streaming
   - Handle call termination

## Troubleshooting

### Common Issues
1. **500 Internal Server Error on WebSocket**
   - Verify media streaming is enabled on Infobip
   - Check WebSocket URL format
   - Verify ngrok connection

2. **Webhook Not Receiving Events**
   - Verify ngrok is running
   - Check webhook URL in settings
   - Verify Infobip webhook configuration

## API Documentation

- [Infobip Voice API](https://www.infobip.com/docs/api/channels/voice)
- [ElevenLabs WebSocket API](https://elevenlabs.io/docs/api-reference/websocket)

## Support

For issues related to:
- Infobip integration: Contact Infobip support
- ElevenLabs integration: Contact ElevenLabs support
- Application issues: Create a GitHub issue

## License

[Your License Here]