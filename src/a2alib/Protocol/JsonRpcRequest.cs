using System.Text.Json;
using System.Text.Json.Nodes;

public class JsonRpcRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public object? Params { get; set; }

    public static JsonRpcRequest Load(JsonNode jsonNode)
    {
        var request = new JsonRpcRequest();
        if (jsonNode is JsonObject jsonObject)
        {
            request.Id = jsonObject["id"]?.ToString() ?? string.Empty;
            request.Method = jsonObject["method"]?.ToString() ?? string.Empty;
            request.Params = jsonObject["params"];
        }
        return request;
    }
    public void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", JsonRpc);
        writer.WriteString("id", Id);
        writer.WriteString("method", Method);
        if (Params != null)
        {
            JsonSerializer.Serialize(writer, Params, options);
        }
        writer.WriteEndObject();
    }
}

public class JsonRpcResponse
{
    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = string.Empty;
    public object? Result { get; set; }
    public JsonRpcError? Error { get; set; }

    public static JsonRpcResponse Load(JsonNode jsonNode)
    {
        var response = new JsonRpcResponse();
        if (jsonNode is JsonObject jsonObject)
        {
            response.Id = jsonObject["id"]?.ToString() ?? string.Empty;
            response.JsonRpc = jsonObject["jsonrpc"]?.ToString() ?? string.Empty;
            response.Result = jsonObject["result"];
            if (jsonObject["error"] != null)
            {
                response.Error = JsonRpcError.Load(jsonObject["error"]);
            }
        }
        return response;
    }

    public void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", JsonRpc);
        writer.WriteString("id", Id);
        if (Error != null)
        {
            writer.WritePropertyName("error");
            Error.Write(writer);
        }
        else if (Result != null)
        {
            JsonSerializer.Serialize(writer, Result, options);
        }
        writer.WriteEndObject();
    }
}
public class JsonRpcError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public JsonNode? Data { get; set; }

    public static JsonRpcError Load(JsonNode jsonNode)
    {
        var error = new JsonRpcError();
        if (jsonNode is JsonObject jsonObject)
        {
            error.Code = jsonObject["code"]?.GetValue<int>() ?? 0;
            error.Message = jsonObject["message"]?.ToString() ?? string.Empty;
            error.Data = jsonObject["data"];
        }
        return error;
    }
     
    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteNumber("code", Code);
        writer.WriteString("message", Message);
        if (Data != null)
        {
            JsonSerializer.Serialize(writer, Data);
        }
        writer.WriteEndObject();
    }
}