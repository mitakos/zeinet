using Microsoft.Extensions.Options;
using ZEIage.Services;
using ZEIage.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Configure settings from appsettings.json
builder.Services.Configure<InfobipSettings>(
    builder.Configuration.GetSection("InfobipSettings"));
builder.Services.Configure<ElevenLabsSettings>(
    builder.Configuration.GetSection("ElevenLabsSettings"));

// Register services with dependency injection
builder.Services.AddHttpClient<InfobipService>();  // HTTP client for Infobip API
builder.Services.AddHttpClient<ElevenLabsService>();  // HTTP client for ElevenLabs API
builder.Services.AddSingleton<WebSocketManager>();  // Manages WebSocket connections
builder.Services.AddScoped<CallController>();  // Main call control
builder.Services.AddLogging();  // Logging service
builder.Services.AddSingleton<SessionManager>();  // Manages call sessions
builder.Services.AddHostedService<ConversationUpdateService>();  // Background service for conversation updates

// Add API controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.UseAuthorization();
app.MapControllers();

app.Run();
