this is example from official infobip website
first go throuhg 
https://www.infobip.com/docs/api/channels/voice/calls



https://www.infobip.com/docs/api/channels/voice/calls/call-legs/create-call
https://www.infobip.com/docs/essentials/api-essentials/api-authentication#api-key-header

EXAMPLE FOR BASIC CALL REQUEST

var options = new RestClientOptions("")
{
    MaxTimeout = -1,
};
var client = new RestClient(options);
var request = new RestRequest("https://yp1341.api.infobip.com/calls/1/calls", Method.Post);
request.AddHeader("Authorization", "{authorization}");
request.AddHeader("Content-Type", "application/json");
request.AddHeader("Accept", "application/json");
var body = @"{""endpoint"":{""type"":""PHONE"",""phoneNumber"":""41792036727""},""from"":""41793026834"",""callsConfigurationId"":""dc5942707c704551a00cd2ea"",""platform"":{""applicationId"":""61c060db2675060027d8c7a6""}}";
request.AddStringBody(body, DataFormat.Json);
RestResponse response = await client.ExecuteAsync(request);
Console.WriteLine(response.Content);



CALL REQUEST WITH PHONE ENDPOINT
var options = new RestClientOptions("")
{
    MaxTimeout = -1,
};
var client = new RestClient(options);
var request = new RestRequest("https://yp1341.api.infobip.com/calls/1/calls", Method.Post);
request.AddHeader("Authorization", "{authorization}");
request.AddHeader("Content-Type", "application/json");
request.AddHeader("Accept", "application/json");
var body = @"{""endpoint"":{""type"":""PHONE"",""phoneNumber"":""41792036727""},""from"":""41793026834"",""connectTimeout"":30,""recording"":{""recordingType"":""AUDIO""},""maxDuration"":300,""callsConfigurationId"":""dc5942707c704551a00cd2ea"",""platform"":{""applicationId"":""61c060db2675060027d8c7a6""}}";
request.AddStringBody(body, DataFormat.Json);
RestResponse response = await client.ExecuteAsync(request);
Console.WriteLine(response.Content);