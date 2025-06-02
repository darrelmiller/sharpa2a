using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class FilePart : Part
{
    [JsonPropertyName("file")]
    public FileWithBytes File { get; set; } = new FileWithBytes();
}


