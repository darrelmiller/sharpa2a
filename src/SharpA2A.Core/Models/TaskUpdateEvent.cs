using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public abstract class TaskUpdateEvent : IJsonRpcOutgoingResult
{
    public string Id { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskUpdateEvent LoadDerived(JsonElement eventElement, ValidationContext context)
    {
        TaskUpdateEvent taskUpdateEvent;
        if (eventElement.TryGetProperty("status", out var statusElement))
        {
            taskUpdateEvent = TaskStatusUpdateEvent.Load(eventElement, context);
        }
        else
        {
            taskUpdateEvent = TaskArtifactUpdateEvent.Load(eventElement, context);
        }
        return taskUpdateEvent;
    }

    public void WriteBase(Utf8JsonWriter writer)
    {
        writer.WriteString("id", Id);
        if (SessionId != null)
        {
            writer.WriteString("sessionId", SessionId);
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
    }

    public abstract void Write(Utf8JsonWriter writer);

}


