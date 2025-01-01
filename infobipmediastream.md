https://www.infobip.com/docs/api/channels/voice/calls/media-stream
Calls API allows your application to stream outbound call media to an arbitrary host. Before initiating the stream, you will need to create at least one new media-stream configuration. After creating the configuration, use the configuration ID within a call to start/stop streaming media. Media is streamed as a series of raw bytes. Currently only audio is supported.

1. Setup media stream configuration (only once) name "ZEI Project Media Stream"

check https://www.infobip.com/docs/api/channels/voice/calls/media-stream/create-media-stream-config


name "ZEI Project Media Stream"
endpoint: Set this to your WebSocket server's URL  *check appsettings.json" and appsettings.Development.json
mediaStreamType: Use "RAW" for raw audio streaming
audioProperties:
encoding: Set to "PCM_LINEAR_16" for 16-bit PCM audio
sampleRate: Use 8000 for standard telephony quality
mediaDirection: Set to "BIDIRECTIONAL" for two-way audio streaming



example C# code from infobip

var options = new RestClientOptions("")
{
    MaxTimeout = -1,
};
var client = new RestClient(options);
var request = new RestRequest("https://yp1341.api.infobip.com/calls/1/media-stream-configs", Method.Post);
request.AddHeader("Authorization", "{authorization}");
request.AddHeader("Content-Type", "application/json");
request.AddHeader("Accept", "application/json");
var body = @"{""name"":""Media-stream config"",""url"":""ws://example-web-socket.com:3001"",""securityConfig"":{""username"":""my-username"",""password"":""my-password"",""type"":""BASIC""}}";
request.AddStringBody(body, DataFormat.Json);
RestResponse response = await client.ExecuteAsync(request);
Console.WriteLine(response.Content);


Store the configuration ID:
When you create the configuration, Infobip will return a configuration ID. Store this ID securely in your application's configuration


2. Get media stream configuration *"ZEI Project Media Stream"
https://www.infobip.com/docs/api/channels/voice/calls/media-stream/get-media-stream-config


var options = new RestClientOptions("")
{
    MaxTimeout = -1,
};
var client = new RestClient(options);
var request = new RestRequest("https://yp1341.api.infobip.com/calls/1/media-stream-configs/{mediaStreamConfigId}", Method.Get);
request.AddHeader("Authorization", "{authorization}");
request.AddHeader("Accept", "application/json");
RestResponse response = await client.ExecuteAsync(request);
Console.WriteLine(response.Content);

Reuse the configuration:
When initiating a call or connecting an existing call to your WebSocket server, use the stored configuration ID


NOTE
When placing outbound calls, include the media-stream configuration ID in the API request.


3. START MEDIA STREAM WHEN CALL IS ESTABLISHED
check https://www.infobip.com/docs/api/channels/voice/calls/call-legs/start-media-stream
Endpoint: https://yp1341.api.infobip.com/calls/1/calls/{callId}/start-media-stream

Start media stream example C# code

var options = new RestClientOptions("")
{
    MaxTimeout = -1,
};
var client = new RestClient(options);
var request = new RestRequest("https://yp1341.api.infobip.com/calls/1/calls/{callId}/start-media-stream", Method.Post);
request.AddHeader("Authorization", "{authorization}");
request.AddHeader("Content-Type", "application/json");
request.AddHeader("Accept", "application/json");
var body = @"{""mediaStream"":{""audioProperties"":{""mediaStreamConfigId"":""63467c6e2885a5389ba11d80"",""replaceMedia"":false}}}";
request.AddStringBody(body, DataFormat.Json);
RestResponse response = await client.ExecuteAsync(request);
Console.WriteLine(response.Content);
