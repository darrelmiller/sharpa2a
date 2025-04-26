using System.Net;
using A2ALib;
using A2ATransport;
using Json.Schema;
using System.Text.Json;

public class MessageTests : IClassFixture<JsonSchemaFixture> {

     private readonly JsonSchema a2aSchema;

    public MessageTests(JsonSchemaFixture fixture) {
        a2aSchema = fixture.Schema;
    }
    
    [Fact]
    public async Task TestGetTask() {
        // Arrange
        var mockHandler = new MockMessageHandler();
        var client = new A2AClient(new HttpClient(mockHandler){
            BaseAddress = new Uri("http://example.org")
        });
        var taskId = "test-task";

        // Act
        var result = await client.GetTask(taskId);
        var message = await mockHandler.Request.Content.ReadAsStringAsync();
        // Assert
        Assert.NotNull(message);

        // JSON Schema validation using JSONSchema.Net
        var json = JsonDocument.Parse(message);
        var validationResult = a2aSchema.Evaluate(json.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(validationResult.IsValid, $"JSON does not match schema: {validationResult.Details}");
    }

    [Fact]
    public async Task TestSendMessage() {
        // Arrange
        var mockHandler = new MockMessageHandler();
        var client = new A2AClient(new HttpClient(mockHandler){
            BaseAddress = new Uri("http://example.org")
        });
        var taskSendParams = new TaskSendParams {
            Id = "test-task",
            Message = new Message()
            {
                Parts =
                [
                    new TextPart()
                    {
                        Type =  "text",
                        Text = "Hello, World!",
                    }
                ],
            },
        };

        // Act
        var result = await client.Send(taskSendParams);
        var message = await mockHandler.Request.Content.ReadAsStringAsync();
        
        // Assert
        Assert.NotNull(message);

        // JSON Schema validation using JSONSchema.Net
        var json = JsonDocument.Parse(message);
        var validationResult = a2aSchema.Evaluate(json.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(validationResult.IsValid, $"JSON does not match schema: {validationResult.Details}");
    }

    [Fact]
    public async Task TestCancelTask() {
        // Arrange
        var mockHandler = new MockMessageHandler();
        var client = new A2AClient(new HttpClient(mockHandler){
            BaseAddress = new Uri("http://example.org")
        });
        var taskId = "test-task";

        // Act
        var result = await client.CancelTask(new TaskIdParams { Id = taskId });
        var message = await mockHandler.Request.Content.ReadAsStringAsync();
        
        // Assert
        Assert.NotNull(message);

        // JSON Schema validation using JSONSchema.Net
        var json = JsonDocument.Parse(message);
        var validationResult = a2aSchema.Evaluate(json.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(validationResult.IsValid, $"JSON does not match schema: {validationResult.Details}");
    }

    [Fact]
    public async Task TestSetPushNotification() {
        // Arrange
        var mockHandler = new MockMessageHandler();
        var client = new A2AClient(new HttpClient(mockHandler){
            BaseAddress = new Uri("http://example.org")
        });
        var pushNotificationConfig = new TaskPushNotificationConfig
        {
         Id = "test-task",
         PushNotificationConfig = new PushNotificationConfig()
         {
             Url = "http://example.org/notify",
             Token = "test-token",
             Authentication = new AuthenticationInfo()
             {
                 Schemes = new List<string> { "Bearer" },
             }
         }
        };

        // Act
        var result = await client.SetPushNotification(pushNotificationConfig);
        var message = await mockHandler.Request.Content.ReadAsStringAsync();
        
        // Assert
        Assert.NotNull(message);

        // JSON Schema validation using JSONSchema.Net
        var json = JsonDocument.Parse(message);
        var validationResult = a2aSchema.Evaluate(json.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(validationResult.IsValid, $"JSON does not match schema: {validationResult.Details}");
    }
}

public class JsonSchemaFixture {
    public JsonSchema Schema { get; }

    public JsonSchemaFixture()
    {
        var schemaText = File.ReadAllText("a2a.json");
        Schema = JsonSchema.FromText(schemaText);
    }
}
public class MockMessageHandler : HttpMessageHandler
{

    public HttpRequestMessage? Request { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
           RequestMessage = request,
           Content = new JsonRpcContent(new JsonRpcResponse()
           {
               Id = "dummy-id",
               Result = new AgentTask()
               {
                   Id = "test-task",
               }
           })
        };
        return Task.FromResult(response);
    }
    
}