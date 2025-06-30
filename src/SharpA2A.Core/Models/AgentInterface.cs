using System.Text.Json.Serialization;

public class AgentInterface
{
    /// <summary>
    /// The transport of the preferred endpoint. If empty, defaults to JSONRPC.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url;

    /// <summary>
    /// Announcement of additional supported transports. Client can use any of
    /// the supported transports.
    /// </summary>
    [JsonPropertyName("transport")]
    public string? Transport;
}