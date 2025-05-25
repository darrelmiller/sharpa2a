using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;


public class JsonRpcResponse
{ 
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

}

public class JsonRpcErrorResponse : JsonRpcResponse
{
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

}
public class JsonRpcResponse<T> : JsonRpcResponse
{
    public static JsonRpcResponse<T> CreateJsonRpcResponse(string requestId, T result)
    {
        return new JsonRpcResponse<T>()
        {
            Id = requestId,
            Result = result,
            JsonRpc = "2.0"
        };
    }


    [JsonPropertyName("result")]
    public T? Result { get; set; }

}

// public class JsonRpcResponseConverter<T> : JsonConverter<JsonRpcResponse<T>>
// {
//     public override JsonRpcResponse<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         using (JsonDocument document = JsonDocument.ParseValue(ref reader))
//         {
//             var rootElement = document.RootElement;

//             var jsonRpc = rootElement.GetProperty("jsonrpc").GetString();
//             var id = rootElement.GetProperty("id").GetString();

//             JsonRpcResponse<T> response = new JsonRpcResponse<T>
//             {
//                 JsonRpc = jsonRpc ?? "2.0",
//                 Id = id ?? string.Empty
//             };

//             if (rootElement.TryGetProperty("result", out var resultProperty))
//             {
//                 response.Result = JsonSerializer.Deserialize<T>(resultProperty.GetRawText(), options);
//             }

//             return response;
//         }
//     }

//     public override void Write(Utf8JsonWriter writer, JsonRpcResponse<T> value, JsonSerializerOptions options)
//     {
//         writer.WriteStartObject();

//         writer.WriteString("jsonrpc", value.JsonRpc);
//         writer.WriteString("id", value.Id);

//         if (value.Result != null)
//         {
//             writer.WritePropertyName("result");
//             JsonSerializer.Serialize(writer, value.Result, value.Result.GetType(), options);
//         }

//         writer.WriteEndObject();
//     }
// }
