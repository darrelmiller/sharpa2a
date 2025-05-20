using System.Text.Json;

namespace SharpA2A.Core;

internal class JsonRpcParams : IJsonRpcIncomingParams
{
    private readonly JsonElement jsonElement;

    public JsonRpcParams(JsonElement jsonElement)
    {
        this.jsonElement = jsonElement;
    }

    public JsonElement Value => this.jsonElement;

}
