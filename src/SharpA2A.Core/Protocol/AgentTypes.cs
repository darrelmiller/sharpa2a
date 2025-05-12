using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

/// <summary>
/// Information about the agent provider
/// </summary>
public class AgentProvider
{
    /// <summary>
    /// The organization name
    /// </summary>
    [JsonPropertyName("organization")]
    [Required]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL for the provider
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Capabilities of an agent
/// </summary>
public class AgentCapabilities
{
    /// <summary>
    /// Whether the agent supports streaming
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; }

    /// <summary>
    /// Whether the agent supports push notifications
    /// </summary>
    [JsonPropertyName("pushNotifications")]
    public bool PushNotifications { get; set; }

    /// <summary>
    /// Whether the agent supports state transition history
    /// </summary>
    [JsonPropertyName("stateTransitionHistory")]
    public bool StateTransitionHistory { get; set; }
}

/// <summary>
/// Authentication information for an agent
/// </summary>
public class AgentAuthentication
{
    /// <summary>
    /// The authentication schemes supported
    /// </summary>
    [JsonPropertyName("schemes")]
    [Required]
    public List<string> Schemes { get; set; } = new List<string>();

    /// <summary>
    /// Optional credentials for authentication
    /// </summary>
    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}

/// <summary>
/// Information about a skill provided by an agent
/// </summary>
public class AgentSkill
{
    /// <summary>
    /// The skill identifier
    /// </summary>
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The skill name
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the skill
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Optional tags for the skill
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Optional examples of using the skill
    /// </summary>
    [JsonPropertyName("examples")]
    public List<string>? Examples { get; set; }

    /// <summary>
    /// Optional input modes supported by the skill
    /// </summary>
    [JsonPropertyName("inputModes")]
    public List<string>? InputModes { get; set; }

    /// <summary>
    /// Optional output modes supported by the skill
    /// </summary>
    [JsonPropertyName("outputModes")]
    public List<string>? OutputModes { get; set; }
}

/// <summary>
/// Information about an agent
/// </summary>
public class AgentCard
{
    /// <summary>
    /// The agent name
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the agent
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The URL for accessing the agent
    /// </summary>
    [JsonPropertyName("url")]
    [Required]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional provider information
    /// </summary>
    [JsonPropertyName("provider")]
    public AgentProvider? Provider { get; set; }

    /// <summary>
    /// The agent version
    /// </summary>
    [JsonPropertyName("version")]
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Optional documentation URL
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// The agent capabilities
    /// </summary>
    [JsonPropertyName("capabilities")]
    [Required]
    public AgentCapabilities Capabilities { get; set; } = new AgentCapabilities();

    /// <summary>
    /// Optional authentication information
    /// </summary>
    [JsonPropertyName("authentication")]
    public AgentAuthentication? Authentication { get; set; }

    /// <summary>
    /// Default input modes supported
    /// </summary>
    [JsonPropertyName("defaultInputModes")]
    public List<string> DefaultInputModes { get; set; } = new List<string> { "text" };

    /// <summary>
    /// Default output modes supported
    /// </summary>
    [JsonPropertyName("defaultOutputModes")]
    public List<string> DefaultOutputModes { get; set; } = new List<string> { "text" };

    /// <summary>
    /// The skills provided by this agent
    /// </summary>
    [JsonPropertyName("skills")]
    [Required]
    public List<AgentSkill> Skills { get; set; } = new List<AgentSkill>();
}
