using DomFactory;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public enum TaskState
{
    [JsonPropertyName("submitted")]
    Submitted,
    [JsonPropertyName("working")]
    Working,
    [JsonPropertyName("input-required")]
    InputRequired,
    [JsonPropertyName("completed")]
    Completed,
    [JsonPropertyName("canceled")]
    Canceled,
    [JsonPropertyName("failed")]
    Failed,
    [JsonPropertyName("unknown")]
    Unknown
}


