using System.Net.ServerSentEvents;

namespace A2ALib;

public interface IA2AClient
{
    Task<AgentTask?> Send(TaskSendParams taskSendParams);
    Task<AgentTask?> GetTask(string taskId);
    Task<AgentTask?> CancelTask(TaskIdParams taskIdParams);
    Task<SseItem<TaskUpdateEvent>> SendSubscribe(TaskSendParams taskSendParams);
    Task<TaskPushNotificationConfig?> SetPushNotification(TaskPushNotificationConfig? pushNotificationConfig);
    Task<TaskPushNotificationConfig?> GetPushNotification(TaskIdParams? taskIdParams);
}
