using System.Text.Json;
using System.Text.Json.Nodes;
using DomFactory;

public interface IJsonRpcParams
{
}

public interface IJsonRpcIncomingParams : IJsonRpcParams
{
    JsonElement Value { get; }
}

public interface IJsonRpcOutgoingParams : IJsonRpcParams
{
    void Write(Utf8JsonWriter writer);
}

public interface IJsonRpcResult
{
}
public interface IJsonRpcIncomingResult : IJsonRpcResult
{
    JsonElement Value { get; }
}
public interface IJsonRpcOutgoingResult : IJsonRpcResult
{
    void Write(Utf8JsonWriter writer);
}

internal class JsonRpcParams : IJsonRpcIncomingParams {
    private readonly JsonElement jsonElement;

    public JsonRpcParams(JsonElement jsonElement)
    {
        this.jsonElement = jsonElement;
    }

    public JsonElement Value => this.jsonElement;

}

public class JsonRpcResult : IJsonRpcIncomingResult, IJsonRpcOutgoingResult {
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

public class JsonRpcRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public IJsonRpcParams? Params { get; set; }

    public static  JsonRpcRequest Load(JsonElement jsonElement, ValidationContext context)
    {
        var request = new JsonRpcRequest();
        ParsingHelpers.ParseMap<JsonRpcRequest>(jsonElement, request, _handlers, context);
        return request;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", JsonRpc);
        writer.WriteString("id", Id);
        writer.WriteString("method", Method);
        if (Params != null)
        {
            writer.WritePropertyName("params");
            ((IJsonRpcOutgoingParams)Params).Write(writer);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<JsonRpcRequest> _handlers = new()
    {
        { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
        { new("jsonrpc"), (ctx, o, e) => o.JsonRpc = e.Value.GetString()! },
        { new("method"), (ctx, o, e) => o.Method = e.Value.GetString()! },
        { new("params"), (ctx, o, e) => o.Params = new JsonRpcParams(e.Value) }
    };
}
