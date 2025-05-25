using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class Message : A2AResponse
{
    [JsonPropertyName("role")]
    [JsonRequired]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("parts")]
    [JsonRequired]
    public List<Part> Parts { get; set; } = new List<Part>();

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    [JsonPropertyName("referenceTaskIds")]
    public List<string>? ReferenceTaskIds { get; set; }

    [JsonPropertyName("messageId")]
    [JsonRequired]
    public string? MessageId { get; set; }

    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }
    [JsonPropertyName("contextId")]
    public string? ContextId { get; set; }

}


