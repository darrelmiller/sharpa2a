using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class AgentTask : IJsonRpcOutgoingResult
{
    public string Id { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    public List<Artifact>? Artifacts { get; set; }
    public List<Message>? History { get; set; } = [];
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public JsonElement Value => throw new NotImplementedException();

    public static AgentTask Load(JsonElement taskElement, ValidationContext context)
    {
        var task = new AgentTask();
        ParsingHelpers.ParseMap<AgentTask>(taskElement, task, _handlers, context);
        return task;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        if (SessionId != null)
        {
            writer.WriteString("sessionId", SessionId);
        }
        if (Status != null)
        {
            writer.WritePropertyName("status");
            Status.Write(writer);
        }

        if (Artifacts != null)
        {
            writer.WritePropertyName("artifacts");
            writer.WriteStartArray();
            foreach (var artifact in Artifacts)
            {
                artifact.Writer(writer);
            }
            writer.WriteEndArray();
        }
        if (History != null)
        {
            writer.WritePropertyName("history");
            writer.WriteStartArray();
            foreach (var message in History)
            {
                message.Write(writer);
            }
            writer.WriteEndArray();
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
    private static readonly FixedFieldMap<AgentTask> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("status"), (ctx, o, e) => o.Status = AgentTaskStatus.Load(e.Value, ctx) },
            { new("artifacts"), (ctx, o, e) => o.Artifacts = ParsingHelpers.GetList(e.Value, Artifact.Load, ctx) },
            { new("history"), (ctx, o, e) => o.History = ParsingHelpers.GetList(e.Value, Message.Load, ctx) },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}


