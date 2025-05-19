using DomFactory;
using System.Text.Json;

namespace SharpA2A.Core;

public class AuthenticationInfo
{
    public List<string> Schemes { get; set; } = new List<string>();
    public string? Credentials { get; set; }

    public static AuthenticationInfo Load(JsonElement authElement, ValidationContext context)
    {
        var authInfo = new AuthenticationInfo();
        ParsingHelpers.ParseMap<AuthenticationInfo>(authElement, authInfo, _handlers, context);
        return authInfo;
    }

    public void Writer(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("schemes");
        writer.WriteStartArray();
        foreach (var scheme in Schemes)
        {
            writer.WriteStringValue(scheme);
        }
        writer.WriteEndArray();
        if (Credentials != null)
        {
            writer.WriteString("credentials", Credentials);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<AuthenticationInfo> _handlers = new() {
            { new("schemes"), (ctx, o, e) => o.Schemes = ParsingHelpers.GetListOfString(e.Value) },
            { new("credentials"), (ctx, o, e) => o.Credentials = e.Value.GetString() }
        };
}


