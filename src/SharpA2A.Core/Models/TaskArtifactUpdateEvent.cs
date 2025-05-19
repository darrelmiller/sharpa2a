using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class TaskArtifactUpdateEvent : TaskUpdateEvent
{
    public Artifact Artifact { get; set; } = new Artifact();

    public static TaskArtifactUpdateEvent Load(JsonElement eventElement, ValidationContext context)
    {
        var taskArtifactUpdateEvent = new TaskArtifactUpdateEvent();
        ParsingHelpers.ParseMap<TaskArtifactUpdateEvent>(eventElement, taskArtifactUpdateEvent, _handlers, context);
        return taskArtifactUpdateEvent;
    }

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteBase(writer);
        if (Artifact != null)
        {
            writer.WritePropertyName("artifact");
            Artifact.Writer(writer);
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskArtifactUpdateEvent> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("artifact"), (ctx, o, e) => o.Artifact = Artifact.Load(e.Value, ctx) }
    };


}


