using System.Text.Json.Serialization;

namespace ZEIage.Models.Infobip;

public class AudioStreamConfig
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "LINEAR16";

    [JsonPropertyName("sampleRate")]
    public int SampleRate { get; set; } = 8000;

    [JsonPropertyName("channels")]
    public int Channels { get; set; } = 1;

    [JsonPropertyName("bitsPerSample")]
    public int BitsPerSample { get; set; } = 16;

    [JsonPropertyName("endianness")]
    public string Endianness { get; set; } = "LITTLE_ENDIAN";
} 