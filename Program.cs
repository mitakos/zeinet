using Microsoft.Extensions.Options;
using ZEIage.Services;
using ZEIage.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<InfobipSettings>(
    builder.Configuration.GetSection("InfobipSettings"));
builder.Services.Configure<ElevenLabsSettings>(
    builder.Configuration.GetSection("ElevenLabsSettings"));

// Register services
builder.Services.AddHttpClient<InfobipService>();
builder.Services.AddHttpClient<ElevenLabsService>();
builder.Services.AddSingleton<ZEIage.Services.WebSocketManager>();
builder.Services.AddScoped<CallController>();
builder.Services.AddLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
