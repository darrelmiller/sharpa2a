using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class AuthenticationInfo
{
    [JsonPropertyName("schemes")]
    [JsonRequired]
    public List<string> Schemes { get; set; } = new List<string>();

    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}


