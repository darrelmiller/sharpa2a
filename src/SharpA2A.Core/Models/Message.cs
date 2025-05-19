using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class Message
{
    public string Role { get; set; } = string.Empty;
    public List<Part> Parts { get; set; } = new List<Part>();
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static Message Load(JsonElement messageElement, ValidationContext context)
    {
        var message = new Message();
        ParsingHelpers.ParseMap<Message>(messageElement, message, _handlers, context);
        return message;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("role", Role);
        if (Parts != null)
        {
            writer.WritePropertyName("parts");
            writer.WriteStartArray();
            foreach (var part in Parts)
            {
                part.Write(writer);
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

    private static readonly FixedFieldMap<Message> _handlers = new() {
            { new("role"), (ctx, o, e) => o.Role = e.Value.GetString()! },
            { new("parts"), (ctx, o, e) => o.Parts = ParsingHelpers.GetList(e.Value, Part.LoadDerived, ctx) },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => {return ie;}, ctx) }
        };

}


