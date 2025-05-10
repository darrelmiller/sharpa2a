using System.Diagnostics;
using A2ALib;

public class EchoAgent
{
    private ITaskManager _TaskManager = new NoopTaskManager();

    public void Attach(TaskManager taskManager)
    {
        _TaskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTask;
        taskManager.OnTaskUpdated = ExecuteAgentTask;
    }

    public async Task ExecuteAgentTask(AgentTask task) {

        if (_TaskManager == null) {
            throw new Exception("TaskManager is not attached.");
        }

        // Set Status to working
        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Working);

        // Process the message
        var message = task.History?.Last().Parts.First().AsTextPart().Text;

        // Create and return an artifact
        var artifact = new Artifact() {
            Parts = [new TextPart() {
                Text = $"Echo: {message}"
            }]
        };
        await _TaskManager.ReturnArtifactAsync(new TaskIdParams() {Id = task.Id}, artifact);

        // Complete the task
        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Completed, final: true);
    }
}