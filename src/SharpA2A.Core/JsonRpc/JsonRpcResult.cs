using System.Text.Json;

namespace SharpA2A.Core;

public class JsonRpcResult : IJsonRpcIncomingResult, IJsonRpcOutgoingResult
{
    private readonly JsonElement jsonElement;

    public JsonElement Value => jsonElement;

    public JsonRpcResult(JsonElement jsonElement)
    {
        this.jsonElement = jsonElement;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteRawValue(jsonElement.ToString());
    }
}
