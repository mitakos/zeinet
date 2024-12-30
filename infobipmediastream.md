https://www.infobip.com/docs/api/channels/voice/calls/media-stream
Calls API allows your application to stream outbound call media to an arbitrary host. Before initiating the stream, you will need to create at least one new media-stream configuration. After creating the configuration, use the configuration ID within a call to start/stop streaming media. Media is streamed as a series of raw bytes. Currently only audio is supported.


https://www.infobip.com/docs/api/channels/voice/calls/media-stream/get-media-stream-configs
curl -L 'https://yp1341.api.infobip.com/calls/1/media-stream-configs' \
-H 'Authorization: {authorization}' \
-H 'Accept: application/json'



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


