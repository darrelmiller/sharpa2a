namespace SharpA2A.Core;

public interface ITaskManager
{
    Func<AgentTask, Task> OnTaskCreated { get; set; }
    Func<AgentTask, Task> OnTaskCancelled { get; set; }
    Func<AgentTask, Task> OnTaskUpdated { get; set; }

    Task<AgentTask?> CancelTaskAsync(TaskIdParams? taskIdParams);
    Task<TaskPushNotificationConfig?> GetPushNotificationAsync(TaskIdParams? taskIdParams);
    Task<AgentTask?> GetTaskAsync(TaskIdParams? taskIdParams);
    Task ReturnArtifactAsync(TaskIdParams taskIdParams, Artifact artifact);
    Task<AgentTask?> SendAsync(TaskSendParams taskSendParams);
    Task<IAsyncEnumerable<TaskUpdateEvent>> SendSubscribeAsync(TaskSendParams taskSendParams);
    Task<TaskPushNotificationConfig?> SetPushNotificationAsync(TaskPushNotificationConfig? pushNotificationConfig);
    Task UpdateStatusAsync(string taskId, TaskState status, Message? message = null, bool final = false);
}
