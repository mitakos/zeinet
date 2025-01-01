using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ZEIage.Services;
using ZEIage.WebSockets;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;
using WebSocketManager = ZEIage.Services.WebSocketManager;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for ngrok
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear networks because we trust ngrok's proxy
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

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

// Configure JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
    });

// Configure services
builder.Services.Configure<InfobipSettings>(builder.Configuration.GetSection("InfobipSettings"));
builder.Services.Configure<ElevenLabsSettings>(builder.Configuration.GetSection("ElevenLabsSettings"));

// Configure HTTP client
builder.Services.AddHttpClient();

// Configure request logging for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    });
}

// Register services
builder.Services.AddSingleton<InfobipService>();
builder.Services.AddSingleton<ElevenLabsService>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddHostedService<ConversationUpdateService>();

var app = builder.Build();

// Configure forwarded headers early in the pipeline
app.UseForwardedHeaders();

// Development specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Don't redirect to HTTPS in development
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next();
    });
}
else
{
    app.UseHttpsRedirection();
}

// Use CORS before other middleware
app.UseCors();

// Configure WebSocket middleware
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};
app.UseWebSockets(webSocketOptions);

app.UseAuthorization();
app.MapControllers();

// Start the application
await app.RunAsync();
