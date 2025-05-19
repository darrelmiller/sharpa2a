using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public abstract class Part
{
    public string Type { get; set; } = "text";
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static Part LoadDerived(JsonElement partElement, ValidationContext context)
    {
        Part part;

        if (partElement.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (type == "text")
            {
                part = TextPart.Load(partElement, context);
            }
            else if (type == "file")
            {
                part = FilePart.Load(partElement, context);
            }
            else if (type == "data")
            {
                part = DataPart.Load(partElement, context);
            }
            else
            {
                throw new InvalidOperationException($"Unknown part type: {type}");
            }
        }
        else
        {
            throw new InvalidOperationException("Part type is required.");
        }
        return part;
    }

    public abstract void Write(Utf8JsonWriter writer);
    internal void WriteBase(Utf8JsonWriter writer)
    {
        if (Type != null)
        {
            writer.WriteString("type", Type);
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
    }

    public TextPart AsTextPart()
    {
        if (this is TextPart textPart)
        {
            return textPart;
        }
        else
        {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to TextPart.");
        }
    }
    public FilePart AsFilePart()
    {
        if (this is FilePart filePart)
        {
            return filePart;
        }
        else
        {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to FilePart.");
        }
    }
    public DataPart AsDataPart()
    {
        if (this is DataPart dataPart)
        {
            return dataPart;
        }
        else
        {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to DataPart.");
        }
    }
}


