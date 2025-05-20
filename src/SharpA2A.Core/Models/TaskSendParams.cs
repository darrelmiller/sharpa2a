using DomFactory;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

[JsonConverter(typeof(TaskSendParamsConverter))]
public class TaskSendParams : IJsonRpcOutgoingParams
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public Message Message { get; set; } = new Message();
    public List<string>? AcceptedOutputModes { get; set; }
    public PushNotificationConfig? PushNotification { get; set; }
    public int? HistoryLength { get; set; }
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskSendParams Load(JsonElement paramsElement, ValidationContext context)
    {
        var taskSendParams = new TaskSendParams();
        ParsingHelpers.ParseMap<TaskSendParams>(paramsElement, taskSendParams, _handlers, context);
        return taskSendParams;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        writer.WriteString("sessionId", SessionId);
        if (Message != null)
        {
            writer.WritePropertyName("message");
            Message.Write(writer);
        }
        if (AcceptedOutputModes != null)
        {
            writer.WritePropertyName("acceptedOutputModes");
            writer.WriteStartArray();
            foreach (var mode in AcceptedOutputModes)
            {
                writer.WriteStringValue(mode);
            }
            writer.WriteEndArray();
        }
        if (PushNotification != null)
        {
            writer.WritePropertyName("pushNotification");
            PushNotification.Write(writer);
        }
        if (HistoryLength != null)
        {
            writer.WriteNumber("historyLength", HistoryLength.Value);
        }
        if (Metadata != null)
        {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskSendParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString()! },
            { new("message"), (ctx, o, e) => o.Message = Message.Load(e.Value, ctx) },
            { new("acceptedOutputModes"), (ctx, o, e) => o.AcceptedOutputModes = ParsingHelpers.GetListOfString(e.Value) },
            { new("pushNotification"), (ctx, o, e) => o.PushNotification = PushNotificationConfig.Load(e.Value, ctx) },
            { new("historyLength"), (ctx, o, e) => o.HistoryLength = e.Value.GetInt32() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}

 public class TaskSendParamsConverter : JsonConverter<TaskSendParams>
    {
        public override TaskSendParams? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object for TaskSendParams");
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            var element = doc.RootElement;

            // Use the existing Load method with a ValidationContext
            return TaskSendParams.Load(element, new ValidationContext("1.0"));
        }

        public override void Write(Utf8JsonWriter writer, TaskSendParams value, JsonSerializerOptions options)
        {
            // Use the existing Write method
            value.Write(writer);
        }
    }

