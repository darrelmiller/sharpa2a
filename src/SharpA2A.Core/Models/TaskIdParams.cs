using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class TaskIdParams : IJsonRpcOutgoingParams
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskIdParams Load(JsonElement paramsElement, ValidationContext context)
    {
        var taskIdParams = new TaskIdParams();
        ParsingHelpers.ParseMap<TaskIdParams>(paramsElement, taskIdParams, _handlers, context);
        return taskIdParams;
    }

    internal void WriteBase(Utf8JsonWriter writer)
    {
        writer.WriteString("id", Id);
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
    public virtual void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteBase(writer);
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskIdParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}


