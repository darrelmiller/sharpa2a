using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class DataPart : Part
{
    [JsonPropertyName("data")]
    public Dictionary<string, JsonElement> Data { get; set; } = new Dictionary<string, JsonElement>();
}


