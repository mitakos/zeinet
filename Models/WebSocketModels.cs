using System.Net.WebSockets;

namespace ZEIage.Models;

public class AudioStreamConfig
{
    public string Format { get; set; } = "LINEAR16";
    public int SampleRate { get; set; } = 8000;
    public int Channels { get; set; } = 1;
    public string Endianness { get; set; } = "LITTLE";
    public string Direction { get; set; } = "BOTH";
}

public class WebSocketConnectionResponse
{
    public string Status { get; set; } = "CONNECTED";
    public string WebSocketUrl { get; set; } = string.Empty;
} 