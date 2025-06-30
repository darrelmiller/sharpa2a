using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public static class JsonUtilities
{
    /// <summary>
    /// Default JSON serializer options used throughout the A2A library.
    /// Configured with camelCase naming, null value ignoring, and comment handling.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true
    };

}