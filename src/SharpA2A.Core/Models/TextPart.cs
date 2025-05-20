using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class TextPart : Part
{

    public string Text { get; set; } = string.Empty;

    public static TextPart Load(JsonElement part, ValidationContext context)
    {
        var textPart = new TextPart();
        ParsingHelpers.ParseMap<TextPart>(part, textPart, _handlers, context);
        return textPart;
    }
    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteBase(writer);
        if (Text != null)
        {
            writer.WriteString("text", Text);
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TextPart> _handlers = new() {
            { new("type"), (ctx, o, e) => o.Type = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => ie, ctx) },
            { new("text"), (ctx, o, e) => o.Text = e.Value.GetString()! }
        };
}


