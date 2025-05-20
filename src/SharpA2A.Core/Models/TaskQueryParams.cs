using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class TaskQueryParams : TaskIdParams, IJsonRpcOutgoingParams
{
    public int? HistoryLength { get; set; }

    public new static TaskQueryParams Load(JsonElement paramsElement, ValidationContext context)
    {
        var taskQueryParams = new TaskQueryParams();
        ParsingHelpers.ParseMap<TaskQueryParams>(paramsElement, taskQueryParams, _handlers, context);
        return taskQueryParams;
    }

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteBase(writer);
        if (HistoryLength != null)
        {
            writer.WriteNumber("historyLength", HistoryLength.Value);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskQueryParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("historyLength"), (ctx, o, e) => o.HistoryLength = e.Value.GetInt32() }
        };
}


