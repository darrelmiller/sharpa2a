using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class DataPart : Part
{
    public Dictionary<string, JsonElement> Data { get; set; } = new Dictionary<string, JsonElement>();

    public static DataPart Load(JsonElement part, ValidationContext context)
    {
        var dataPart = new DataPart();
        ParsingHelpers.ParseMap<DataPart>(part, dataPart, _handlers, context);
        return dataPart;
    }

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (Data != null)
        {
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (var kvp in Data)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<DataPart> _handlers = new() {
            { new("type"), (ctx, o, e) => o.Type = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => ie, ctx) },
            { new("data"), (ctx, o, e) => o.Data = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
     };
}


