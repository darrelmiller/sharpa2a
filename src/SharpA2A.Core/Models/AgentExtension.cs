using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public class AgentExtension
{
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    public string? Required { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, JsonNode>? Params { get; set; }
}