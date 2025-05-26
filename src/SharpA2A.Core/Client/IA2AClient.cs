using System.Net.ServerSentEvents;

namespace SharpA2A.Core;

public interface IA2AClient
{
    Task<AgentTask> Send(MessageSendParams taskSendParams);
    Task<AgentTask> GetTask(string taskId);
    Task<AgentTask> CancelTask(TaskIdParams taskIdParams);
    IAsyncEnumerable<SseItem<A2AEvent>> SendSubscribe(MessageSendParams taskSendParams);
    Task<TaskPushNotificationConfig> SetPushNotification(TaskPushNotificationConfig pushNotificationConfig);
    Task<TaskPushNotificationConfig> GetPushNotification(TaskIdParams taskIdParams);
}
