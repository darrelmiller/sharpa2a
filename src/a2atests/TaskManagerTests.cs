using A2ALib;

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
        var task = await taskManager.SendAsync(taskSendParams);
        Assert.NotNull(task);
        Assert.Equal("testTask", task.Id);
        Assert.Equal(TaskState.Submitted, task.Status.State);
    }


    [Fact]
    public async Task CreateAndRetreiveTask() {
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
    public async Task CancelTask() {
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
    public async Task UpdateTask() {
        var taskManager = new TaskManager()
        {
            OnTaskCreated = (task) =>
            {
                task.Status.State = TaskState.Submitted;
            },
            OnTaskUpdated = (task) =>
            {
                task.Status.State = TaskState.Working;
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
    public async Task UpdateTaskStatus() {
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

        await taskManager.UpdateStatus(task.Id, TaskState.Completed,new Message
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
    public async Task ReturnArtifactSync() {
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
        await taskManager.ReturnArtifact(new TaskIdParams { Id = "testTask" }, artifact);
        await taskManager.UpdateStatus("testTask", TaskState.Completed);
        var completedTask = await taskManager.GetTaskAsync(new TaskIdParams { Id = "testTask" });
        Assert.NotNull(completedTask);
        Assert.Equal("testTask", completedTask.Id);
        Assert.Equal(TaskState.Completed, completedTask.Status.State);
        Assert.NotNull(completedTask.Artifacts);
        Assert.Equal(1, completedTask.Artifacts.Count);
        Assert.Equal("Test Artifact", completedTask.Artifacts[0].Name);
    }
}
