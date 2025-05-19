using System.Text.Json;
using DomFactory;

namespace SharpA2A.Core;

public static class JsonRpcErrorResponses
{
    public static JsonRpcResponse InvalidParamsResponse(string requestId) => new()
    {
        Id = requestId,
        Error = new InvalidParamsError(),
        JsonRpc = "2.0"
    };

    public static JsonRpcResponse MethodNotFoundResponse(string requestId) => new()
    {
        Id = requestId,
        Error = new JsonRpcError
        {
            Code = -32601,
            Message = "Method not found"
        },
        JsonRpc = "2.0"
    };

    public static JsonRpcResponse InternalErrorResponse(string requestId, string message) => new()
    {
        Id = requestId,
        Error = new JsonRpcError
        {
            Code = -32603,
            Message = message
        },
        JsonRpc = "2.0"
    };

    public static JsonRpcResponse ParseErrorResponse(string requestId, string message) => new()
    {
        Id = requestId,
        Error = new JsonRpcError
        {
            Code = -32700,
            Message = message
        },
        JsonRpc = "2.0"
    };
}


public class JsonRpcError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }

    public static JsonRpcError Load(JsonElement jsonElement, ValidationContext context)
    {
        var error = new JsonRpcError();
        ParsingHelpers.ParseMap<JsonRpcError>(jsonElement, error, _handlers, context);
        return error;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteNumber("code", Code);
        writer.WriteString("message", Message);
        if (Data != null)
        {
            writer.WritePropertyName("data");
            writer.WriteRawValue(Data.Value.ToString());
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<JsonRpcError> _handlers = new()
    {
        { new("code"), (ctx, o, e) => o.Code = e.Value.GetInt32() },
        { new("message"), (ctx, o, e) => o.Message = e.Value.GetString()! },
        { new("data"), (ctx, o, e) => o.Data = e.Value }
    };
}