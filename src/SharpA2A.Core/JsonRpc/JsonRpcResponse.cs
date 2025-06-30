using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;


public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public JsonNode? Result { get; set; }

    public static JsonRpcResponse CreateJsonRpcResponse<T>(string requestId, T result)
    {
        JsonNode? node = null;
        if (result != null) {
            node = JsonSerializer.SerializeToNode(result, JsonUtilities.DefaultSerializerOptions);
        }
        return new JsonRpcResponse()
        {
            Id = requestId,
            JsonRpc = "2.0",
            Result = node
        };
    }
}

public class JsonRpcErrorResponse : JsonRpcResponse
{
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

}