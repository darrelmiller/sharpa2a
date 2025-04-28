using A2ALib;

public class EchoAgent
{
    private TaskManager? _TaskManager;

    public void Attach(TaskManager taskManager)
    {
        _TaskManager = taskManager;
        taskManager.OnTaskCreated = async (task) => {
            await ExecuteAgentTask(task);
        };
        taskManager.OnTaskUpdated = async (task) => {
            await ExecuteAgentTask(task);
        };
    }

    public async Task ExecuteAgentTask(AgentTask task) {
        if  (_TaskManager == null) {
            throw new Exception("TaskManager is not attached.");
        }

        var message = task.History!.Last().Parts.First().AsTextPart().Text;
        var artifact = new Artifact() {
            Parts = [new TextPart() {
                Text = $"Echo: {message}"
            }]
        };
        await _TaskManager.ReturnArtifact(new TaskIdParams() {Id = task.Id}, artifact);
    }
}