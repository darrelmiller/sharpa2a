using System.IO.Pipelines;
using System.Net;
using System.Net.ServerSentEvents;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace A2ALib;

public class TaskManager
{
    private HttpClient _CallbackHttpClient;
    private ITaskStore _TaskStore;
    /// <summary>
    /// Agent handler for task creation.
    /// </summary>
    public Action<AgentTask> OnTaskCreated { get; set; } = (task) => { };
    /// <summary>
    /// Agent handler for task cancellation.
    /// </summary>
    public Action<AgentTask> OnTaskCancelled { get; set; } = (task) => { };
    /// <summary>
    /// Agent handler for task update.
    /// </summary>
    public Action<AgentTask> OnTaskUpdated { get; set; } = (task) => { };

    public TaskManager( HttpClient? callbackHttpClient = null, ITaskStore? taskStore = null)
    {
        _CallbackHttpClient = callbackHttpClient ?? new HttpClient();
        _TaskStore = taskStore ?? new InMemoryTaskStore();
    }

    public async Task<AgentTask?> CancelTaskAsync(TaskIdParams? taskIdParams)
    {
        var task = await _TaskStore.GetTaskAsync(taskIdParams.Id);
        if (task != null)
        {
            await _TaskStore.UpdateStatusAsync(task.Id, TaskState.Canceled);
            OnTaskCancelled(task);
            return task;
        }
        else
        {
            throw new ArgumentException("Task not found or invalid TaskIdParams.");
        }
    }

    public Task<AgentTask?> GetTaskAsync(TaskIdParams? taskIdParams)
    {
        if (taskIdParams == null)
        {
            throw new ArgumentNullException(nameof(taskIdParams), "TaskIdParams cannot be null.");
        }
        return _TaskStore.GetTaskAsync(taskIdParams.Id);
    }

    public async Task<AgentTask?> SendAsync(TaskSendParams taskSendParams)
    {
        if (taskSendParams == null)
        {
            throw new ArgumentNullException(nameof(taskSendParams), "TaskSendParams cannot be null.");
        }
        var task = await _TaskStore.GetTaskAsync(taskSendParams.Id);

        if(task == null)
        {
            task = new AgentTask
            {
                Id = taskSendParams.Id,
                SessionId = taskSendParams.SessionId,
                History = [taskSendParams.Message],
                Status = new AgentTaskStatus()
                {
                    State = TaskState.Submitted,
                    Timestamp = DateTime.UtcNow
                },
                Metadata = taskSendParams.Metadata
            };
            _TaskStore.SetTaskAsync(task);
            OnTaskCreated(task);
        } else {
            task.History.Add(taskSendParams.Message);
            _TaskStore.SetTaskAsync(task);
            OnTaskUpdated(task);
        }
        return task;
    }

    public Task<IAsyncEnumerable<TaskUpdateEvent>> SendSubscribeAsync(TaskSendParams taskSendParams)
    {
        throw new NotImplementedException();
    }

    public async Task<TaskPushNotificationConfig?> SetPushNotificationAsync(TaskPushNotificationConfig? pushNotificationConfig)
    {
        if (pushNotificationConfig != null)
        {
            await _TaskStore.SetPushNotificationConfigAsync(pushNotificationConfig);
            return pushNotificationConfig;
        }
        else
        {
            throw new ArgumentException("Missing push notification config.");
        }
    }

    public async Task<TaskPushNotificationConfig?> GetPushNotificationAsync(TaskIdParams? taskIdParams)
    {
        var pushNotificationConfig = await _TaskStore.GetPushNotificationAsync(taskIdParams.Id);
        return pushNotificationConfig;
    }

    /// <summary>
    /// Updates the status of a task. This is used by the agent to update the status of a task.
    /// </summary>
    /// <remarks>
    /// Should this be limited to only allow certain state transitions?
    /// </remarks>
    /// <param name="taskId"></param>
    /// <param name="status"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task UpdateStatus(string taskId, TaskState status, Message? message = null)
    {
        await _TaskStore.UpdateStatusAsync(taskId, status, message);
        //TODO: Make callback notification if set by the client
        //TODO: If open stream for the task return a TaskStatusUpdateEvent
    }

    /// <summary>
    /// Enables an agent to add an artifact to a task to be returned to the client.
    /// </summary>
    /// <param name="taskIdParams"></param>
    /// <param name="artifact"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task ReturnArtifact(TaskIdParams taskIdParams, Artifact artifact)
    {
        if (artifact == null)
        {
            throw new ArgumentNullException(nameof(artifact), "Artifact cannot be null.");
        }

        var task = await _TaskStore.GetTaskAsync(taskIdParams.Id);
        if (task != null)
        {
            if (task.Artifacts == null)
            {
                task.Artifacts = new List<Artifact>();
            }
            task.Artifacts.Add(artifact);
            await _TaskStore.SetTaskAsync(task);
            //TODO: Make callback notification if set by the client
            //TODO: If open stream for the task return a TaskStatusUpdateEvent
        }
        else
        {
            throw new ArgumentException("Task not found.");
        }
    }
    // TODO: Implement UpdateArtifact method
}

