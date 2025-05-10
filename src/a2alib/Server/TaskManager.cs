using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace A2ALib;

public class TaskManager
{
    // OpenTelemetry ActivitySource
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.TaskManager", "1.0.0");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private HttpClient _CallbackHttpClient;
    private ITaskStore _TaskStore;
    /// <summary>
    /// Agent handler for task creation.
    /// </summary>
    public Func<AgentTask, Task> OnTaskCreated { get; set; } = (task) => { return Task.CompletedTask; };
    /// <summary>
    /// Agent handler for task cancellation.
    /// </summary>
    public Func<AgentTask, Task> OnTaskCancelled { get; set; } = (task) => { return Task.CompletedTask; };
    /// <summary>
    /// Agent handler for task update.
    /// </summary>
    public Func<AgentTask, Task> OnTaskUpdated { get; set; } = (task) => { return Task.CompletedTask; };

    private Dictionary<string, TaskUpdateEventEnumerator> _TaskUpdateEventEnumerators = new Dictionary<string, TaskUpdateEventEnumerator>();

    public TaskManager(HttpClient? callbackHttpClient = null, ITaskStore? taskStore = null)
    {
        _CallbackHttpClient = callbackHttpClient ?? new HttpClient();
        _TaskStore = taskStore ?? new InMemoryTaskStore();
    }
    public async Task<AgentTask?> CancelTaskAsync(TaskIdParams? taskIdParams)
    {
        if (taskIdParams == null)
        {
            throw new ArgumentNullException(nameof(taskIdParams), "TaskIdParams cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("CancelTask", ActivityKind.Server);
        activity?.SetTag("task.id", taskIdParams.Id);

        var task = await _TaskStore.GetTaskAsync(taskIdParams.Id);
        if (task != null)
        {
            activity?.SetTag("task.found", true);
            await _TaskStore.UpdateStatusAsync(task.Id, TaskState.Canceled);
            await OnTaskCancelled(task);
            return task;
        }
        else
        {
            activity?.SetTag("task.found", false);
            throw new ArgumentException("Task not found or invalid TaskIdParams.");
        }
    }
    public async Task<AgentTask?> GetTaskAsync(TaskIdParams? taskIdParams)
    {
        if (taskIdParams == null)
        {
            throw new ArgumentNullException(nameof(taskIdParams), "TaskIdParams cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("GetTask", ActivityKind.Server);
        activity?.SetTag("task.id", taskIdParams.Id);

        var task = await _TaskStore.GetTaskAsync(taskIdParams.Id);
        activity?.SetTag("task.found", task != null);
        return task;
    }
    public async Task<AgentTask?> SendAsync(TaskSendParams taskSendParams)
    {
        if (taskSendParams == null)
        {
            throw new ArgumentNullException(nameof(taskSendParams), "TaskSendParams cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("SendTask", ActivityKind.Server);
        activity?.SetTag("task.id", taskSendParams.Id);
        activity?.SetTag("task.sessionId", taskSendParams.SessionId);

        var task = await _TaskStore.GetTaskAsync(taskSendParams.Id);

        if (task == null)
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
            await _TaskStore.SetTaskAsync(task);
            using (var createActivity = ActivitySource.StartActivity("OnTaskCreated", ActivityKind.Server))
            {
                await OnTaskCreated(task);
            }

        }
        else
        {
            if (task.History == null)
            {
                task.History = new List<Message>();
            }
            task.History.Add(taskSendParams.Message);
            await _TaskStore.SetTaskAsync(task);
            using (var createActivity = ActivitySource.StartActivity("OnTaskUpdated", ActivityKind.Server))
            {
                await OnTaskUpdated(task);
            }
        }
        return task;
    }
    public async Task<IAsyncEnumerable<TaskUpdateEvent>> SendSubscribeAsync(TaskSendParams taskSendParams)
    {
        if (taskSendParams == null)
        {
            throw new ArgumentNullException(nameof(taskSendParams), "TaskSendParams cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("SendSubscribe", ActivityKind.Server);
        activity?.SetTag("task.id", taskSendParams.Id);
        activity?.SetTag("task.sessionId", taskSendParams.SessionId);

        Task processingTask;
        var agentTask = await _TaskStore.GetTaskAsync(taskSendParams.Id);
        if (agentTask == null)
        {
            agentTask = new AgentTask
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
            await _TaskStore.SetTaskAsync(agentTask);
            processingTask = Task.Run(async () =>
            {
                using (var createActivity = ActivitySource.StartActivity("OnTaskUpdated", ActivityKind.Server))
                {
                    await OnTaskCreated(agentTask);
                }
            });

        }
        else
        {
            if (agentTask.History == null)
            {
                agentTask.History = new List<Message>();
            }
            agentTask.History.Add(taskSendParams.Message);
            await _TaskStore.SetTaskAsync(agentTask);
            processingTask = Task.Run(async () =>
            {
                using (var createActivity = ActivitySource.StartActivity("OnTaskUpdated", ActivityKind.Server))
                {
                    await OnTaskUpdated(agentTask);
                }
            });
        }

        var enumerator = new TaskUpdateEventEnumerator(processingTask);
        _TaskUpdateEventEnumerators[taskSendParams.Id] = enumerator;
        return enumerator;
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
        if (taskIdParams == null)
        {
            throw new ArgumentNullException(nameof(taskIdParams), "TaskIdParams cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("GetPushNotification", ActivityKind.Server);
        activity?.SetTag("task.id", taskIdParams.Id);

        var pushNotificationConfig = await _TaskStore.GetPushNotificationAsync(taskIdParams.Id);
        activity?.SetTag("config.found", pushNotificationConfig != null);
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
    public async Task UpdateStatusAsync(string taskId, TaskState status, Message? message = null, bool final = false)
    {
        using var activity = ActivitySource.StartActivity("UpdateStatus", ActivityKind.Server);
        activity?.SetTag("task.id", taskId);
        activity?.SetTag("task.status", status.ToString());
        activity?.SetTag("task.finalStatus", final);

        try
        {
            var agentStatus = await _TaskStore.UpdateStatusAsync(taskId, status, message);
            //TODO: Make callback notification if set by the client
            _TaskUpdateEventEnumerators.TryGetValue(taskId, out var enumerator);
            if (enumerator != null)
            {
                var taskUpdateEvent = new TaskStatusUpdateEvent
                {
                    Id = taskId,
                    Status = agentStatus,
                    Final = final
                };
                if (final)
                {
                    activity?.SetTag("event.type", "final");
                    enumerator.NotifyFinalEvent(taskUpdateEvent);
                }
                else
                {
                    activity?.SetTag("event.type", "update");
                    enumerator.NotifyEvent(taskUpdateEvent);
                }
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Enables an agent to add an artifact to a task to be returned to the client.
    /// </summary>
    /// <param name="taskIdParams"></param>
    /// <param name="artifact"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task ReturnArtifactAsync(TaskIdParams taskIdParams, Artifact artifact)
    {
        if (artifact == null)
        {
            throw new ArgumentNullException(nameof(artifact), "Artifact cannot be null.");
        }

        using var activity = ActivitySource.StartActivity("ReturnArtifact", ActivityKind.Server);
        activity?.SetTag("task.id", taskIdParams.Id);

        try
        {
            var task = await _TaskStore.GetTaskAsync(taskIdParams.Id);
            if (task != null)
            {
                activity?.SetTag("task.found", true);

                if (task.Artifacts == null)
                {
                    task.Artifacts = new List<Artifact>();
                }
                task.Artifacts.Add(artifact);
                await _TaskStore.SetTaskAsync(task);

                //TODO: Make callback notification if set by the client
                _TaskUpdateEventEnumerators.TryGetValue(task.Id, out var enumerator);
                if (enumerator != null)
                {
                    var taskUpdateEvent = new TaskArtifactUpdateEvent
                    {
                        Id = task.Id,
                        Artifact = artifact
                    };
                    activity?.SetTag("event.type", "artifact");
                    enumerator.NotifyEvent(taskUpdateEvent);
                }
            }
            else
            {
                activity?.SetTag("task.found", false);
                activity?.SetStatus(ActivityStatusCode.Error, "Task not found");
                throw new ArgumentException("Task not found.");
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
    // TODO: Implement UpdateArtifact method
}
