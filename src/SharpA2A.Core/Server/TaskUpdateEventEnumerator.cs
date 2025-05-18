
using System.Collections.Concurrent;

namespace SharpA2A.Core;

public class TaskUpdateEventEnumerator : IAsyncEnumerable<TaskUpdateEvent>
{
    private bool isFinal = false;
    private ConcurrentQueue<TaskUpdateEvent> _UpdateEvents = new ConcurrentQueue<TaskUpdateEvent>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
    private readonly Task processingTask;

    public TaskUpdateEventEnumerator(Task processingTask)
    {
        this.processingTask = processingTask;  // Store the processing task so it doesn't get garbage collected
    }
    public void NotifyEvent(TaskUpdateEvent taskUpdateEvent)
    {
        // Enqueue the event to the queue
        _UpdateEvents.Enqueue(taskUpdateEvent);
        _semaphore.Release();
    }

    public void NotifyFinalEvent(TaskUpdateEvent taskUpdateEvent)
    {
        isFinal = true;
        // Enqueue the final event to the queue
        _UpdateEvents.Enqueue(taskUpdateEvent);
        _semaphore.Release();
    }

    public async IAsyncEnumerator<TaskUpdateEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (!isFinal || !_UpdateEvents.IsEmpty)
        {
            // Wait for an event to be available
            await _semaphore.WaitAsync(cancellationToken);
            if (_UpdateEvents.TryDequeue(out var taskUpdateEvent))
            {
                yield return taskUpdateEvent;
            }
        }
    }
}

