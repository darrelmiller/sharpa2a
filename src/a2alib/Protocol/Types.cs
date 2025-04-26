using System.Text.Json.Serialization;

namespace A2ALib;

public enum TaskState {
    [JsonPropertyName("submitted")]
    Submitted,
    [JsonPropertyName("working")]
    Working,
    [JsonPropertyName("input-required")]
    InputRequired,
    [JsonPropertyName("completed")]
    Completed,
    [JsonPropertyName("canceled")]
    Canceled,
    [JsonPropertyName("failed")]
    Failed,
    [JsonPropertyName("unknown")]
    Unknown
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextPart), typeDiscriminator: "text")]
[JsonDerivedType(typeof(FilePart), typeDiscriminator: "file")]
[JsonDerivedType(typeof(DataPart), typeDiscriminator: "data")]
public abstract class Part {}


public class TextPart : Part {
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class FileContent {
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
    [JsonPropertyName("bytes")]
    public string? Bytes { get; set; }
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}

public class FilePart : Part {
    [JsonPropertyName("type")]
    public string Type { get; set; } = "file";
    [JsonPropertyName("file")]
    public FileContent File { get; set; } = new FileContent();
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class DataPart : Part {
    [JsonPropertyName("type")]
    public string Type { get; set; } = "data";
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class Message {
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new List<Part>();
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class AgentTaskStatus {
    [JsonPropertyName("state")]
    public TaskState State { get; set; }
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Artifact {
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("parts")]
    public List<object> Parts { get; set; } = new List<object>();
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;
    [JsonPropertyName("append")]
    public bool? Append { get; set; }
    [JsonPropertyName("lastChunk")]
    public bool? LastChunk { get; set; }
}

public class AgentTask {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
    [JsonPropertyName("status")]
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    [JsonPropertyName("artifacts")]
    public List<Artifact>? Artifacts { get; set; }
    [JsonPropertyName("history")]
    public List<Message>? History { get; set; } = [];
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class TaskUpdateEvent {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
public class TaskStatusUpdateEvent : TaskUpdateEvent {
    [JsonPropertyName("status")]
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    [JsonPropertyName("final")]
    public bool Final { get; set; } = false;
}

public class TaskArtifactUpdateEvent {
    [JsonPropertyName("artifact")]
    public Artifact Artifact { get; set; } = new Artifact();
}
public class AuthenticationInfo {
    [JsonPropertyName("schemes")]
    public List<string> Schemes { get; set; } = new List<string>();
    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}
public class PushNotificationConfig {
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    [JsonPropertyName("authentication")]
    public AuthenticationInfo? Authentication { get; set; }
}
public class TaskIdParams {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
public class TaskQueryParams : TaskIdParams {
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }
}
public class TaskSendParams {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("message")]
    public Message Message { get; set; } = new Message();
    [JsonPropertyName("acceptedOutputModes")]
    public List<string>? AcceptedOutputModes { get; set; }
    [JsonPropertyName("pushNotification")]
    public PushNotificationConfig? PushNotification { get; set; }
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class TaskPushNotificationConfig {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("pushNotificationConfig")]
    public PushNotificationConfig PushNotificationConfig { get; set; } = new PushNotificationConfig();
}
public class JSONRPCMessage {
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";
    [JsonPropertyName("id")]
    public object? Id { get; set; } = Guid.NewGuid().ToString("N");
}

