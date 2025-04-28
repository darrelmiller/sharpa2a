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

public class JsonRpcResponse
{
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