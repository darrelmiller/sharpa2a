using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class Artifact
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Part> Parts { get; set; } = new List<Part>();
    public Dictionary<string, JsonElement>? Metadata { get; set; }
    public int Index { get; set; } = 0;
    public bool? Append { get; set; }
    public bool? LastChunk { get; set; }

    public static Artifact Load(JsonElement artifactElement, ValidationContext context)
    {
        var artifact = new Artifact();
        ParsingHelpers.ParseMap<Artifact>(artifactElement, artifact, _handlers, context);
        return artifact;
    }

    public void Writer(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (Name != null)
        {
            writer.WriteString("name", Name);
        }
        if (Description != null)
        {
            writer.WriteString("description", Description);
        }
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
        if (Index != 0)
        {
            writer.WriteNumber("index", Index);
        }
        if (Append != null)
        {
            writer.WriteBoolean("append", Append.Value);
        }
        if (LastChunk != null)
        {
            writer.WriteBoolean("lastChunk", LastChunk.Value);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<Artifact> _handlers = new() {
            { new("name"), (ctx, o, e) => o.Name = e.Value.GetString()! },
            { new("description"), (ctx, o, e) => o.Description = e.Value.GetString()! },
            { new("parts"), (ctx, o, e) => o.Parts = ParsingHelpers.GetList(e.Value, Part.LoadDerived, ctx) },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("index"), (ctx, o, e) => o.Index = e.Value.GetInt32() },
            { new("append"), (ctx, o, e) => o.Append = e.Value.GetBoolean() },
            { new("lastChunk"), (ctx, o, e) => o.LastChunk = e.Value.GetBoolean() }
        };
}


