using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DomFactory;

namespace SharpA2A.Core;

[JsonConverter(typeof(JsonRpcRequestConverter))]
public class JsonRpcRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public IJsonRpcParams? Params { get; set; }

    public static JsonRpcRequest Load(JsonElement jsonElement, ValidationContext context)
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
        { new("params"), (ctx, o, e) => o.Params = new JsonRpcParams(e.Value.Clone()) }
    };
}

public class JsonRpcRequestConverter : JsonConverter<JsonRpcRequest>
{
    public override JsonRpcRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var jsonElement = document.RootElement;

        var context = new ValidationContext("1.0"); // Assuming you have a way to create a ValidationContext
        return JsonRpcRequest.Load(jsonElement, context);
    }

    public override void Write(Utf8JsonWriter writer, JsonRpcRequest value, JsonSerializerOptions options)
    {
        value.Write(writer);
    }
}