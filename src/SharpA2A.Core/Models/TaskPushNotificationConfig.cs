using DomFactory;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class TaskPushNotificationConfig : IJsonRpcOutgoingParams, IJsonRpcOutgoingResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("pushNotificationConfig")]
    public PushNotificationConfig PushNotificationConfig { get; set; } = new PushNotificationConfig();

    public JsonElement Value => throw new NotImplementedException();

    public static TaskPushNotificationConfig Load(JsonElement paramsElement, ValidationContext context)
    {
        var taskPushNotificationConfig = new TaskPushNotificationConfig();
        ParsingHelpers.ParseMap<TaskPushNotificationConfig>(paramsElement, taskPushNotificationConfig, _handlers, context);
        return taskPushNotificationConfig;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        if (PushNotificationConfig != null)
        {
            writer.WritePropertyName("pushNotificationConfig");
            PushNotificationConfig.Write(writer);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskPushNotificationConfig> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("pushNotificationConfig"), (ctx, o, e) => o.PushNotificationConfig = PushNotificationConfig.Load(e.Value, ctx) }
        };
}


