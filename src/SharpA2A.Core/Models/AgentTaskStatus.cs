using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class AgentTaskStatus
{
    public TaskState State { get; set; }
    public Message? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AgentTaskStatus Load(JsonElement statusElement, ValidationContext context)
    {
        var status = new AgentTaskStatus();
        ParsingHelpers.ParseMap<AgentTaskStatus>(statusElement, status, _handlers, context);
        return status;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("state", State.ToString().ToLowerInvariant());
        if (Message != null)
        {
            writer.WritePropertyName("message");
            Message.Write(writer);
        }
        writer.WriteString("timestamp", Timestamp.ToString("o"));
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<AgentTaskStatus> _handlers = new() {
            { new("state"), (ctx, o, e) => o.State = ParsingHelpers.ParseEnums<TaskState>(e.Value, "state", "AgentState", ctx) },
            { new("message"), (ctx, o, e) => o.Message = Message.Load(e.Value, ctx) },
            { new("timestamp"), (ctx, o, e) => o.Timestamp = e.Value.GetDateTime() }
        };
}


