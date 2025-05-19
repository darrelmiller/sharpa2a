using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class FilePart : Part
{
    public FileContent File { get; set; } = new FileContent();

    public static FilePart Load(JsonElement part, ValidationContext context)
    {
        var filePart = new FilePart();
        ParsingHelpers.ParseMap<FilePart>(part, filePart, _handlers, context);
        return filePart;
    }

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteBase(writer);
        if (File != null)
        {
            writer.WritePropertyName("file");
            File.Writer(writer);
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<FilePart> _handlers = new() {
            { new("type"), (ctx, o, e) => o.Type = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => ie, ctx) },
            { new("file"), (ctx, o, e) => o.File = FileContent.Load(e.Value, ctx) }
     };
}


