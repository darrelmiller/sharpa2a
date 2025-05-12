namespace SharpA2A.Core;

public interface ITaskManager
{
    Func<AgentTask, Task> OnTaskCreated { get; set; }
    Func<AgentTask, Task> OnTaskCancelled { get; set; }
    Func<AgentTask, Task> OnTaskUpdated { get; set; }
    Func<string, AgentCard> OnAgentCardQuery { get; set; }

    Task ReturnArtifactAsync(string taskId, Artifact artifact);
    Task UpdateStatusAsync(string taskId, TaskState status, Message? message = null, bool final = false);
}
