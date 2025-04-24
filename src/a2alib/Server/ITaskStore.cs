namespace A2ALib;

public interface ITaskStore
{
    Task<AgentTask?> GetTaskAsync(string taskId);
    Task<TaskPushNotificationConfig?> GetPushNotificationAsync(string taskId);
    Task UpdateStatusAsync(string taskId, TaskState status, Message? message = null);
    Task SetTaskAsync(AgentTask task);
    Task SetPushNotificationConfigAsync(TaskPushNotificationConfig pushNotificationConfig);
}

