using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DomFactory;

namespace SharpA2A.Core;

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

public abstract class Part {
    public string Type { get; set; } = "text";
    public Dictionary<string, JsonElement>? Metadata { get; set; }
  
    public static Part LoadDerived(JsonElement partElement, ValidationContext context) {
        Part part;

        if (partElement.TryGetProperty("type", out var typeElement)) {
            var type = typeElement.GetString();
            if (type == "text") {
                part = TextPart.Load(partElement, context);
            } else if (type == "file") {
                part = FilePart.Load(partElement, context);
            } else if (type == "data") {
                part = DataPart.Load(partElement, context);
            } else {
                throw new InvalidOperationException($"Unknown part type: {type}");
            }
        } else {
            throw new InvalidOperationException("Part type is required.");
        }
        return part;
    }

    public abstract void Write(Utf8JsonWriter writer);
    internal void WriteBase(Utf8JsonWriter writer) {
        if (Type != null) {
            writer.WriteString("type", Type);
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
    }

    public TextPart AsTextPart() {
        if (this is TextPart textPart) {
            return textPart;
        } else {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to TextPart.");
        }
    }
    public FilePart AsFilePart() {
        if (this is FilePart filePart) {
            return filePart;
        } else {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to FilePart.");
        }
    }
    public DataPart AsDataPart() {
        if (this is DataPart dataPart) {
            return dataPart;
        } else {
            throw new InvalidCastException($"Cannot cast {this.GetType()} to DataPart.");
        }
    }
}


public class TextPart : Part {

    public string Text { get; set; } = string.Empty;

    public static TextPart Load(JsonElement part, ValidationContext context) {
        var textPart = new TextPart();
        ParsingHelpers.ParseMap<TextPart>(part, textPart, _handlers, context);
        return textPart;
    }
    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        WriteBase(writer);
        if (Text != null) {
            writer.WriteString("text", Text);
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TextPart> _handlers = new() {
            { new("type"), (ctx, o, e) => o.Type = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => ie, ctx) },
            { new("text"), (ctx, o, e) => o.Text = e.Value.GetString()! }
        };
}

public class FileContent {
    public string? Name { get; set; }
    public string? MimeType { get; set; }
    public string? Bytes { get; set; }
    public string? Uri { get; set; }

    public static FileContent Load(JsonElement fileElement, ValidationContext context) {
        var fileContent = new FileContent();
        ParsingHelpers.ParseMap<FileContent>(fileElement, fileContent, _handlers, context);
        return fileContent;
    }

    public void Writer(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        if (Name != null) {
            writer.WriteString("name", Name);
        }
        if (MimeType != null) {
            writer.WriteString("mimeType", MimeType);
        }
        if (Bytes != null) {
            writer.WriteString("bytes", Bytes);
        }
        if (Uri != null) {
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

public class FilePart : Part {
    public FileContent File { get; set; } = new FileContent();

    public static FilePart Load(JsonElement part, ValidationContext context) {
        var filePart = new FilePart();
        ParsingHelpers.ParseMap<FilePart>(part, filePart, _handlers, context);
        return filePart;
    }

    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        WriteBase(writer);
        if (File != null) {
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

public class DataPart : Part {
    public Dictionary<string, JsonElement> Data { get; set; } = new Dictionary<string, JsonElement>();

    public static DataPart Load(JsonElement part, ValidationContext context) {
        var dataPart = new DataPart();
        ParsingHelpers.ParseMap<DataPart>(part, dataPart, _handlers, context);
        return dataPart;
    }

    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        if (Data != null) {
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (var kvp in Data) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<DataPart> _handlers = new() {
            { new("type"), (ctx, o, e) => o.Type = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie,ctx) => ie, ctx) },
            { new("data"), (ctx, o, e) => o.Data = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
     };
}

public class Message {
    public string Role { get; set; } = string.Empty;
    public List<Part> Parts { get; set; } = new List<Part>();
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static Message Load(JsonElement messageElement, ValidationContext context) {
        var message = new Message();
        ParsingHelpers.ParseMap<Message>(messageElement, message, _handlers, context);
        return message;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("role", Role);
        if (Parts != null) {
            writer.WritePropertyName("parts");
            writer.WriteStartArray();
            foreach (var part in Parts) {
                part.Write(writer);
            }
            writer.WriteEndArray();
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
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

public class AgentTaskStatus {
    public TaskState State { get; set; }
    public Message? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AgentTaskStatus Load(JsonElement statusElement, ValidationContext context) {
        var status = new AgentTaskStatus();
        ParsingHelpers.ParseMap<AgentTaskStatus>(statusElement, status, _handlers, context);
        return status;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("state", State.ToString().ToLowerInvariant());
        if (Message != null) {
            writer.WritePropertyName("message");
            Message.Write(writer);
        }
        writer.WriteString("timestamp", Timestamp.ToString("o"));
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<AgentTaskStatus> _handlers = new() {
            { new("state"), (ctx, o, e) => o.State = ParsingHelpers.ParseEnums<TaskState>(e.Value, "state", "AgentState", ctx) },
            { new("message"), (ctx, o, e) => o.Message = Message.Load(e.Value, ctx) },
            { new("timestamp"), (ctx, o, e) => o.Timestamp = e.Value.GetDateTime() }
        };
}

public class Artifact {
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Part> Parts { get; set; } = new List<Part>();
    public Dictionary<string, JsonElement>? Metadata { get; set; }
    public int Index { get; set; } = 0;
    public bool? Append { get; set; }
    public bool? LastChunk { get; set; }

    public static Artifact Load(JsonElement artifactElement, ValidationContext context) {
        var artifact = new Artifact();
        ParsingHelpers.ParseMap<Artifact>(artifactElement, artifact, _handlers, context);
        return artifact;
    }

    public void Writer(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        if (Name != null) {
            writer.WriteString("name", Name);
        }
        if (Description != null) {
            writer.WriteString("description", Description);
        }
        if (Parts != null) {
            writer.WritePropertyName("parts");
            writer.WriteStartArray();
            foreach (var part in Parts) {
                part.Write(writer);
            }
            writer.WriteEndArray();
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        if (Index != 0) {
            writer.WriteNumber("index", Index);
        }
        if (Append != null) {
            writer.WriteBoolean("append", Append.Value);
        }
        if (LastChunk != null) {
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

public class AgentTask : IJsonRpcOutgoingResult {
    public string Id { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    public List<Artifact>? Artifacts { get; set; }
    public List<Message>? History { get; set; } = [];
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public JsonElement Value => throw new NotImplementedException();

    public static AgentTask Load(JsonElement taskElement, ValidationContext context) {
        var task = new AgentTask();
        ParsingHelpers.ParseMap<AgentTask>(taskElement, task, _handlers, context);
        return task;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        if (SessionId != null) {
            writer.WriteString("sessionId", SessionId);
        }
        if(Status != null) {
            writer.WritePropertyName("status");
            Status.Write(writer);
        }
        
        if (Artifacts != null) {
            writer.WritePropertyName("artifacts");
            writer.WriteStartArray();
            foreach (var artifact in Artifacts) {
                artifact.Writer(writer);
            }
            writer.WriteEndArray();
        }
        if (History != null) {
            writer.WritePropertyName("history");
            writer.WriteStartArray();
            foreach (var message in History) {
                message.Write(writer);
            }
            writer.WriteEndArray();
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<AgentTask> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("status"), (ctx, o, e) => o.Status = AgentTaskStatus.Load(e.Value, ctx) },
            { new("artifacts"), (ctx, o, e) => o.Artifacts = ParsingHelpers.GetList(e.Value, Artifact.Load, ctx) },
            { new("history"), (ctx, o, e) => o.History = ParsingHelpers.GetList(e.Value, Message.Load, ctx) },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}

public abstract class TaskUpdateEvent : IJsonRpcOutgoingResult {
    public string Id { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskUpdateEvent LoadDerived(JsonElement eventElement, ValidationContext context) {
        TaskUpdateEvent taskUpdateEvent;
        if (eventElement.TryGetProperty("status", out var statusElement)) {
                taskUpdateEvent =  TaskStatusUpdateEvent.Load(eventElement, context);
        } else {
            taskUpdateEvent = TaskArtifactUpdateEvent.Load(eventElement, context);
        }
        return taskUpdateEvent;
    }

    public void WriteBase(Utf8JsonWriter writer) {
        writer.WriteString("id", Id);
        if (SessionId != null) {
            writer.WriteString("sessionId", SessionId);
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
    }

    public abstract void Write(Utf8JsonWriter writer);

}
public class TaskStatusUpdateEvent : TaskUpdateEvent {
    public AgentTaskStatus Status { get; set; } = new AgentTaskStatus();
    public bool Final { get; set; } = false;

    public static TaskStatusUpdateEvent Load(JsonElement eventElement, ValidationContext context) {
        var taskStatusUpdateEvent = new TaskStatusUpdateEvent();
        ParsingHelpers.ParseMap<TaskStatusUpdateEvent>(eventElement, taskStatusUpdateEvent, _handlers, context);
        return taskStatusUpdateEvent;
    }

    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        base.WriteBase(writer);
        if(Status != null) {
            writer.WritePropertyName("status");
            Status.Write(writer);
        }
        writer.WriteBoolean("final", Final);
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskStatusUpdateEvent> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("status"), (ctx, o, e) => o.Status = AgentTaskStatus.Load(e.Value, ctx) },
            { new("final"), (ctx, o, e) => o.Final = e.Value.GetBoolean() }
        };
}

public class TaskArtifactUpdateEvent : TaskUpdateEvent {
    public Artifact Artifact { get; set; } = new Artifact();

    public static TaskArtifactUpdateEvent Load(JsonElement eventElement, ValidationContext context) {
        var taskArtifactUpdateEvent = new TaskArtifactUpdateEvent();
        ParsingHelpers.ParseMap<TaskArtifactUpdateEvent>(eventElement, taskArtifactUpdateEvent, _handlers, context);
        return taskArtifactUpdateEvent;
    }

    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        WriteBase(writer);
        if (Artifact != null) {
            writer.WritePropertyName("artifact");
            Artifact.Writer(writer);
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskArtifactUpdateEvent> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("artifact"), (ctx, o, e) => o.Artifact = Artifact.Load(e.Value, ctx) }
    };


}

public class AuthenticationInfo {
    public List<string> Schemes { get; set; } = new List<string>();
    public string? Credentials { get; set; }

    public static AuthenticationInfo Load(JsonElement authElement, ValidationContext context) {
        var authInfo = new AuthenticationInfo();
        ParsingHelpers.ParseMap<AuthenticationInfo>(authElement, authInfo, _handlers, context);
        return authInfo;
    }

    public void Writer(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WritePropertyName("schemes");
        writer.WriteStartArray();
        foreach (var scheme in Schemes) {
            writer.WriteStringValue(scheme);
        }
        writer.WriteEndArray();
        if (Credentials != null) {
            writer.WriteString("credentials", Credentials);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<AuthenticationInfo> _handlers = new() {
            { new("schemes"), (ctx, o, e) => o.Schemes = ParsingHelpers.GetListOfString(e.Value) },
            { new("credentials"), (ctx, o, e) => o.Credentials = e.Value.GetString() }
        };
}
public class PushNotificationConfig : IJsonRpcOutgoingResult {
    public string Url { get; set; } = string.Empty;
    public string? Token { get; set; }
    public AuthenticationInfo? Authentication { get; set; }

    public static PushNotificationConfig Load(JsonElement configElement, ValidationContext context) {
        var pushNotificationConfig = new PushNotificationConfig();
        ParsingHelpers.ParseMap<PushNotificationConfig>(configElement, pushNotificationConfig, _handlers, context);
        return pushNotificationConfig;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("url", Url);
        if (Token != null) {
            writer.WriteString("token", Token);
        }
        if (Authentication != null) {
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
public class TaskIdParams : IJsonRpcOutgoingParams {
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskIdParams Load(JsonElement paramsElement, ValidationContext context) {
        var taskIdParams = new TaskIdParams();
        ParsingHelpers.ParseMap<TaskIdParams>(paramsElement, taskIdParams, _handlers, context);
        return taskIdParams;
    }

    internal void WriteBase(Utf8JsonWriter writer) {
        writer.WriteString("id", Id);
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
    }
    public virtual void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        WriteBase(writer);
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskIdParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}
public class TaskQueryParams : TaskIdParams, IJsonRpcOutgoingParams {
    public int? HistoryLength { get; set; }

    public new static TaskQueryParams Load(JsonElement paramsElement, ValidationContext context) {
        var taskQueryParams = new TaskQueryParams();
        ParsingHelpers.ParseMap<TaskQueryParams>(paramsElement, taskQueryParams, _handlers, context);
        return taskQueryParams;
    }

    public override void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        WriteBase(writer);
        if (HistoryLength != null) {
            writer.WriteNumber("historyLength", HistoryLength.Value);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskQueryParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) },
            { new("historyLength"), (ctx, o, e) => o.HistoryLength = e.Value.GetInt32() }
        };
}
public class TaskSendParams : IJsonRpcOutgoingParams {
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public Message Message { get; set; } = new Message();
    public List<string>? AcceptedOutputModes { get; set; }
    public PushNotificationConfig? PushNotification { get; set; }
    public int? HistoryLength { get; set; }
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public static TaskSendParams Load(JsonElement paramsElement, ValidationContext context) {
        var taskSendParams = new TaskSendParams();
        ParsingHelpers.ParseMap<TaskSendParams>(paramsElement, taskSendParams, _handlers, context);
        return taskSendParams;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        writer.WriteString("sessionId", SessionId);
        if(Message != null) {
            writer.WritePropertyName("message");
            Message.Write(writer);
        }
        if (AcceptedOutputModes != null) {
            writer.WritePropertyName("acceptedOutputModes");
            writer.WriteStartArray();
            foreach (var mode in AcceptedOutputModes) {
                writer.WriteStringValue(mode);
            }
            writer.WriteEndArray();
        }
        if (PushNotification != null) {
            writer.WritePropertyName("pushNotification");
            PushNotification.Write(writer);
        }
        if (HistoryLength != null) {
            writer.WriteNumber("historyLength", HistoryLength.Value);
        }
        if (Metadata != null) {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in Metadata) {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static readonly FixedFieldMap<TaskSendParams> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("sessionId"), (ctx, o, e) => o.SessionId = e.Value.GetString()! },
            { new("message"), (ctx, o, e) => o.Message = Message.Load(e.Value, ctx) },
            { new("acceptedOutputModes"), (ctx, o, e) => o.AcceptedOutputModes = ParsingHelpers.GetListOfString(e.Value) },
            { new("pushNotification"), (ctx, o, e) => o.PushNotification = PushNotificationConfig.Load(e.Value, ctx) },
            { new("historyLength"), (ctx, o, e) => o.HistoryLength = e.Value.GetInt32() },
            { new("metadata"), (ctx, o, e) => o.Metadata = ParsingHelpers.GetMap(e.Value, (ie, ctx) => ie, ctx) }
        };
}


public class TaskPushNotificationConfig : IJsonRpcOutgoingParams, IJsonRpcOutgoingResult {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("pushNotificationConfig")]
    public PushNotificationConfig PushNotificationConfig { get; set; } = new PushNotificationConfig();

    public JsonElement Value => throw new NotImplementedException();

    public static TaskPushNotificationConfig Load(JsonElement paramsElement, ValidationContext context) {
        var taskPushNotificationConfig = new TaskPushNotificationConfig();
        ParsingHelpers.ParseMap<TaskPushNotificationConfig>(paramsElement, taskPushNotificationConfig, _handlers, context);
        return taskPushNotificationConfig;
    }

    public void Write(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        if (PushNotificationConfig != null) {
            writer.WritePropertyName("pushNotificationConfig");
            PushNotificationConfig.Write(writer);
        }
        writer.WriteEndObject();
    }
    private static readonly FixedFieldMap<TaskPushNotificationConfig> _handlers = new() {
            { new("id"), (ctx, o, e) => o.Id = e.Value.GetString()! },
            { new("pushNotificationConfig"), (ctx, o, e) => o.PushNotificationConfig = PushNotificationConfig.Load(e.Value, ctx) }
        };
}


