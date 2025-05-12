using SharpA2A.Core;

public class EchoAgent
{
    private ITaskManager? _TaskManager = null;

    public void Attach(TaskManager taskManager)
    {
        _TaskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTask;
        taskManager.OnTaskUpdated = ExecuteAgentTask;
        taskManager.OnAgentCardQuery = GetAgentCard;
    }

    public async Task ExecuteAgentTask(AgentTask task)
    {

        if (_TaskManager == null)
        {
            throw new Exception("TaskManager is not attached.");
        }

        // Set Status to working
        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Working);

        // Process the message
        var message = task.History?.Last().Parts.First().AsTextPart().Text;

        // Create and return an artifact
        var artifact = new Artifact()
        {
            Parts = [new TextPart() {
                Text = $"Echo: {message}"
            }]
        };
        await _TaskManager.ReturnArtifactAsync(task.Id, artifact);

        // Complete the task
        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Completed, final: true);
    }

    public AgentCard GetAgentCard(string agentUrl)
    {
        var capabilities = new AgentCapabilities()
        {
            Streaming = true,
            PushNotifications = false,
        };

        return new AgentCard()
        {
            Name = "Echo Agent",
            Description = "Agent which will echo every message it receives.",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [],
        };
    }
}