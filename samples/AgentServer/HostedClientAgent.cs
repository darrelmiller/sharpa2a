using A2ALib;


public class HostedClientAgent 
{
    private TaskManager? _TaskManager;
    private A2AClient echoClient;

    public HostedClientAgent() {
        echoClient = new A2AClient(new HttpClient() { BaseAddress = new Uri("http://localhost:5048/echo")});
    }
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

        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Working);

        // Get message from the user to HostedClientAgent
        var userMessage = task.History!.Last().Parts.First().AsTextPart().Text;
        var echoTask = await echoClient.Send(new TaskSendParams() {
            Id = Guid.NewGuid().ToString(),
            Message = new Message() {
                Parts = [new TextPart() {
                    Text = $"HostedClientAgent received {userMessage}"
                }]
            }});

        // Get the the return artifact from the EchoAgent
        var message = echoTask.Artifacts!.Last().Parts.First().AsTextPart().Text;

        // Return as artifact to the HostedClientAgent
        var artifact = new Artifact() {
            Parts = [new TextPart() {
                Text = $"EchoAgent said: {message}"
            }]
        };
        await _TaskManager.ReturnArtifactAsync(new TaskIdParams() {Id = task.Id}, artifact);
        await _TaskManager.UpdateStatusAsync(task.Id, TaskState.Completed);
    }
}