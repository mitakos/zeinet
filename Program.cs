using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ZEIage.Services;
using ZEIage.WebSockets;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;
using WebSocketManager = ZEIage.Services.WebSocketManager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure services
builder.Services.Configure<InfobipSettings>(builder.Configuration.GetSection("InfobipSettings"));
builder.Services.Configure<ElevenLabsSettings>(builder.Configuration.GetSection("ElevenLabsSettings"));

// Validate configuration
var infobipSettings = builder.Configuration.GetSection("InfobipSettings").Get<InfobipSettings>();
var elevenLabsSettings = builder.Configuration.GetSection("ElevenLabsSettings").Get<ElevenLabsSettings>();

if (infobipSettings == null)
{
    throw new InvalidOperationException("InfobipSettings section is missing from configuration");
}

if (elevenLabsSettings == null)
{
    throw new InvalidOperationException("ElevenLabsSettings section is missing from configuration");
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton<InfobipService>();
builder.Services.AddSingleton<ElevenLabsService>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddHostedService<ConversationUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors();

// Configure WebSocket middleware with development settings
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};
app.UseWebSockets(webSocketOptions);

app.UseAuthorization();
app.MapControllers();

// Try different ports if default is taken
var ports = new[] { 5133, 5134, 5135, 5136, 5137 };
foreach (var port in ports)
{
    try
    {
        app.Urls.Clear();
        app.Urls.Add($"http://localhost:{port}");
        await app.RunAsync();
        break;
    }
    catch (IOException) when (port != ports[^1])
    {
        Console.WriteLine($"Port {port} is in use, trying next port...");
        continue;
    }
}
