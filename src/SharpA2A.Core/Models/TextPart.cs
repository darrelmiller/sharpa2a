using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class TextPart : Part
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}


