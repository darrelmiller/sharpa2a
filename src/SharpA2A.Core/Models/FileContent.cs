using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class FileContent
{
    public string? Name { get; set; }
    public string? MimeType { get; set; }
    public string? Bytes { get; set; }
    public string? Uri { get; set; }

    public static FileContent Load(JsonElement fileElement, ValidationContext context)
    {
        var fileContent = new FileContent();
        ParsingHelpers.ParseMap<FileContent>(fileElement, fileContent, _handlers, context);
        return fileContent;
    }

    public void Writer(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (Name != null)
        {
            writer.WriteString("name", Name);
        }
        if (MimeType != null)
        {
            writer.WriteString("mimeType", MimeType);
        }
        if (Bytes != null)
        {
            writer.WriteString("bytes", Bytes);
        }
        if (Uri != null)
        {
            writer.WriteString("uri", Uri);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<FileContent> _handlers = new() {
            { new("name"), (ctx, o, e) => o.Name = e.Value.GetString()! },
            { new("mimeType"), (ctx, o, e) => o.MimeType = e.Value.GetString()! },
            { new("bytes"), (ctx, o, e) => o.Bytes = e.Value.GetString()! },
            { new("uri"), (ctx, o, e) => o.Uri = e.Value.GetString()! }
        };
}


