using System.Diagnostics;
using SharpA2A.Core;

public class EchoAgent
{
    private ITaskManager? _TaskManager = null;

    public void Attach(TaskManager taskManager)
    {
        _TaskManager = taskManager;
        taskManager.OnMessageReceived = ProcessMessage;
        taskManager.OnAgentCardQuery = GetAgentCard;
    }

    public Task<Message> ProcessMessage(MessageSendParams messageSendParams)
    {
        // Process the message
        var messageText = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;

        // Create and return an artifact
        var message = new Message()
        {
            MessageId = Guid.NewGuid().ToString(),
            Parts = [new TextPart() {
                Text = $"Echo: {messageText}"
            }]
        };
        return Task.FromResult(message);
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