using DomFactory;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

[JsonConverter(typeof(PushNotificationConfigConverter))]
public class PushNotificationConfig : IJsonRpcOutgoingResult
{
    public string Url { get; set; } = string.Empty;
    public string? Token { get; set; }
    public AuthenticationInfo? Authentication { get; set; }

    public static PushNotificationConfig Load(JsonElement configElement, ValidationContext context)
    {
        var pushNotificationConfig = new PushNotificationConfig();
        ParsingHelpers.ParseMap<PushNotificationConfig>(configElement, pushNotificationConfig, _handlers, context);
        return pushNotificationConfig;
    }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("url", Url);
        if (Token != null)
        {
            writer.WriteString("token", Token);
        }
        if (Authentication != null)
        {
            writer.WritePropertyName("authentication");
            Authentication.Writer(writer);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<PushNotificationConfig> _handlers = new() {
            { new("url"), (ctx, o, e) => o.Url = e.Value.GetString()! },
            { new("token"), (ctx, o, e) => o.Token = e.Value.GetString() },
            { new("authentication"), (ctx, o, e) => o.Authentication = AuthenticationInfo.Load(e.Value, ctx) }
        };
}


public class PushNotificationConfigConverter : JsonConverter<PushNotificationConfig>
{
    public override PushNotificationConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse the JSON into a JsonElement
        using var document = JsonDocument.ParseValue(ref reader);
        var configElement = document.RootElement;

        // Use the existing Load method for deserialization
        var validationContext = new ValidationContext("1.0"); // Assuming a default constructor exists
        return PushNotificationConfig.Load(configElement, validationContext);
    }

    public override void Write(Utf8JsonWriter writer, PushNotificationConfig value, JsonSerializerOptions options)
    {
        // Use the existing Write method for serialization
        value.Write(writer);
    }
}