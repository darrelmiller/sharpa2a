using SharpA2A.Core;

namespace a2atests;

public class TaskManagerTests
{
    [Fact]
    public async Task CreateTask()
    {
        var taskManager = new TaskManager();
        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        string messageReceived = string.Empty;
        taskManager.OnTaskCreated = (task) =>
        {
            messageReceived = (task.History.Last().Parts[0] as TextPart).Text;
            return Task.CompletedTask;
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);
        Assert.Equal(1, task.History.Count);
        Assert.Equal("Hello, World!", (task.History[0].Parts[0] as TextPart).Text);
        Assert.Equal("Hello, World!", messageReceived);

    }


    [Fact]
    public async Task CreateAndRetreiveTask()
    {
        var taskManager = new TaskManager();
        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);

        var retrievedTask = await taskManager.GetTaskAsync(new TaskIdParams { Id = "testTask" });
        Assert.NotNull(retrievedTask);
        Assert.Equal("testTask", retrievedTask.Id);
        Assert.Equal(TaskState.Submitted, retrievedTask.Status.State);
    }

    [Fact]
    public async Task CancelTask()
    {
        var taskManager = new TaskManager();
        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);

        var cancelledTask = await taskManager.CancelTaskAsync(new TaskIdParams { Id = "testTask" });
        Assert.NotNull(cancelledTask);
        Assert.Equal("testTask", cancelledTask.Id);
        Assert.Equal(TaskState.Canceled, cancelledTask.Status.State);
    }

    [Fact]
    public async Task UpdateTask()
    {
        var taskManager = new TaskManager()
        {
            OnTaskCreated = (task) =>
            {
                task.Status.State = TaskState.Submitted;
                return Task.CompletedTask;
            },
            OnTaskUpdated = (task) =>
            {
                task.Status.State = TaskState.Working;
                return Task.CompletedTask;
            }
        };

        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);

        var updateSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Task updated!"
                    }
                ]
            },
        };
        var updatedTask = await taskManager.SendAsync(updateSendParams);
        Assert.NotNull(updatedTask);
        Assert.Equal("testTask", updatedTask.Id);
        Assert.Equal(TaskState.Working, updatedTask.Status.State);
        Assert.Equal("Task updated!", (updatedTask.History.Last().Parts[0] as TextPart).Text);
    }

    [Fact]
    public async Task UpdateTaskStatus()
    {
        var taskManager = new TaskManager();

        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);

        await taskManager.UpdateStatusAsync(task.Id, TaskState.Completed, new Message
        {
            Parts = [
                    new TextPart
                    {
                        Text = "Task completed!"
                    }
                ]
        }
        );
        var completedTask = await taskManager.GetTaskAsync(new TaskIdParams { Id = "testTask" });
        Assert.NotNull(completedTask);
        Assert.Equal("testTask", completedTask.Id);
        Assert.Equal(TaskState.Completed, completedTask.Status.State);
    }

    [Fact]
    public async Task ReturnArtifactSync()
    {
        var taskManager = new TaskManager();

        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Write me a poem"
                    }
                ]
            },
        };
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);

        var artifact = new Artifact
        {
            Name = "Test Artifact",
            Parts =
            [
                new TextPart
                {
                    Text = "When all at once, a host of golden daffodils,"
                }
            ]
        };
        await taskManager.ReturnArtifactAsync(new TaskIdParams { Id = "testTask" }, artifact);
        await taskManager.UpdateStatusAsync("testTask", TaskState.Completed);
        var completedTask = await taskManager.GetTaskAsync(new TaskIdParams { Id = "testTask" });
        Assert.NotNull(completedTask);
        Assert.Equal("testTask", completedTask.Id);
        Assert.Equal(TaskState.Completed, completedTask.Status.State);
        Assert.NotNull(completedTask.Artifacts);
        Assert.Equal(1, completedTask.Artifacts.Count);
        Assert.Equal("Test Artifact", completedTask.Artifacts[0].Name);
    }

    [Fact]
    public async Task CreateSendSubscribeTask()
    {
        var taskManager = new TaskManager();
        taskManager.OnTaskCreated = async (task) =>
        {
            await taskManager.UpdateStatusAsync(task.Id, TaskState.Working, final: true);
        };

        var taskSendParams = new TaskSendParams
        {
            Id = "testTask",
            Message = new Message
            {
                Parts = [
                    new TextPart
                    {
                        Text = "Hello, World!"
                    }
                ]
            },
        };
        var taskEvents = await taskManager.SendSubscribeAsync(taskSendParams);
        var taskCount = 0;
        await foreach (var taskEvent in taskEvents)
        {
            Assert.NotNull(taskEvent);
            Assert.Equal("testTask", taskEvent.Id);
            var statusEvent = taskEvent as TaskStatusUpdateEvent;
            Assert.Equal(TaskState.Working, statusEvent.Status.State);
            taskCount++;
        }
        Assert.Equal(1, taskCount);

    }

    [Fact]
    public async Task VerifyTaskEventEnumerator()
    {
        var enumerator = new TaskUpdateEventEnumerator(null);

        var task = Task.Run(async () =>
        {
            await Task.Delay(1000);
            enumerator.NotifyEvent(new TaskStatusUpdateEvent
            {
                Id = "testTask",
                Status = new AgentTaskStatus
                {
                    State = TaskState.Working,
                    Timestamp = DateTime.UtcNow
                }
            });

            await Task.Delay(1000);
            enumerator.NotifyFinalEvent(new TaskStatusUpdateEvent
            {
                Id = "testTask",
                Status = new AgentTaskStatus
                {
                    State = TaskState.Completed,
                    Timestamp = DateTime.UtcNow
                }
            });
        });

        var eventCount = 0;
        await foreach (var taskEvent in enumerator)
        {
            Assert.NotNull(taskEvent);
            Assert.IsType<TaskStatusUpdateEvent>(taskEvent);
            eventCount++;
        }
        Assert.Equal(2, eventCount);


    }
}
