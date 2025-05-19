using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class TaskStatusUpdateEvent : TaskUpdateEvent
{
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    public bool Final { get; set; } = false;

    public static TaskStatusUpdateEvent Load(JsonElement eventElement, ValidationContext context)
    {
        var taskStatusUpdateEvent = new TaskStatusUpdateEvent();
        ParsingHelpers.ParseMap<TaskStatusUpdateEvent>(eventElement, taskStatusUpdateEvent, _handlers, context);
        return taskStatusUpdateEvent;
    }

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        base.WriteBase(writer);
        if (Status != null)
        {
            writer.WritePropertyName("status");
            Status.Write(writer);
        }
        writer.WriteBoolean("final", Final);
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskStatusUpdateEvent> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("status"), (ctx, o, e) => o.Status = AgentTaskStatus.Load(e.Value, ctx) },
            { new("final"), (ctx, o, e) => o.Final = e.Value.GetBoolean() }
        };
}


