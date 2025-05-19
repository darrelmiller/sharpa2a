using System.Text.Json;
using DomFactory;

namespace SharpA2A.Core;

public class JsonRpcResponse
{
     public static JsonRpcResponse CreateJsonRpcResponse<T>(string requestId, T result) where T : IJsonRpcResult?
    {
        return new JsonRpcResponse()
        {
            Id = requestId,
            Result = result,
            JsonRpc = "2.0"
        };
    }


    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = string.Empty;
    public IJsonRpcResult? Result { get; set; }
    public JsonRpcError? Error { get; set; }

    public static JsonRpcResponse Load(JsonElement jsonElement, ValidationContext context)
    {
        var response = new JsonRpcResponse();
        ParsingHelpers.ParseMap<JsonRpcResponse>(jsonElement, response, _handlers, context);
        return response;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", JsonRpc);
        writer.WriteString("id", Id);
        if (Result != null)
        {
            writer.WritePropertyName("result");
            ((IJsonRpcOutgoingResult)Result).Write(writer);
        }
        if (Error != null)
        {
            writer.WritePropertyName("error");
            Error.Write(writer);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<JsonRpcResponse> _handlers = new()
    {
        { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
        { new("jsonrpc"), (ctx, o, e) => o.JsonRpc = e.Value.GetString()! },
        { new("result"), (ctx, o, e) => o.Result = new JsonRpcResult(e.Value) },
        { new("error"), (ctx, o, e) => o.Error = JsonRpcError.Load(e.Value,ctx) }
    };
}
