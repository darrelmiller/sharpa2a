
namespace A2ALib;

public class TaskUpdateEventEnumerator : IAsyncEnumerable<TaskUpdateEvent>
{
    private bool isFinal = false;
    private TaskCompletionSource<TaskUpdateEvent> _taskCompletionSource = new TaskCompletionSource<TaskUpdateEvent>();
    private Task processingTask;

    public TaskUpdateEventEnumerator(Task processingTask)
    {
        this.processingTask = processingTask;
    }

    public void NotifyEvent(TaskUpdateEvent taskUpdateEvent)
    {
        _taskCompletionSource.SetResult(taskUpdateEvent);
    }

    public void NotifyFinalEvent(TaskUpdateEvent taskUpdateEvent)
    {
        isFinal = true;
        _taskCompletionSource.SetResult(taskUpdateEvent);
    }
    private Task<TaskUpdateEvent> GetNextEvent()
    {
        return _taskCompletionSource.Task;
    }
    public async IAsyncEnumerator<TaskUpdateEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (!isFinal)
        {
            var taskUpdateEvent = await GetNextEvent();
            yield return taskUpdateEvent;
            // Reset the TaskCompletionSource for the next event.
            _taskCompletionSource = new TaskCompletionSource<TaskUpdateEvent>();
        }
    }
}