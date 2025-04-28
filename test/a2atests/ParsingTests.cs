using System.Runtime.CompilerServices;
using System.Text.Json;
using A2ALib;
using DomFactory;

namespace A2ATests;

public class ParsingTests  {
    
    [Fact]
    public async Task RoundTripTaskSendParams() {
        // Arrange
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
        var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        taskSendParams.Write(writer);
        writer.Flush();
        stream.Position = 0;
        var jsonDoc = await JsonDocument.ParseAsync(stream);
        var context = new ValidationContext("1.0");
        var parsedParams = A2AMethods.ParseParameters(context, A2AMethods.TaskSend, jsonDoc.RootElement);
        
        // Act
        var result = (TaskSendParams)parsedParams;
        
        // Assert
        Assert.Equal(taskSendParams.Id, result.Id);
        Assert.Equal(taskSendParams.Message.Parts[0].AsTextPart().Text, result.Message.Parts[0].AsTextPart().Text);
        Assert.Empty(context.Problems);
    }

    [Fact]
    public async Task JsonRpcTaskSend() {
        // Arrange
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
        var jsonRpcRequest = new JsonRpcRequest {
            Method = A2AMethods.TaskSend,
            Params = taskSendParams,
        };
        var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        jsonRpcRequest.Write(writer);
        writer.Flush();
        stream.Position = 0;
        var jsonDoc = await JsonDocument.ParseAsync(stream);
        var context = new ValidationContext("1.0");
        var rpcRequest = JsonRpcRequest.Load(jsonDoc.RootElement, context);
        IJsonRpcIncomingParams? incomingParams = (IJsonRpcIncomingParams)rpcRequest.Params;
        var parsedParams = A2AMethods.ParseParameters(context, A2AMethods.TaskSend, incomingParams.Value);
        
        // Act
        var result = (TaskSendParams)parsedParams;
        
        // Assert
        Assert.Equal(taskSendParams.Id, result.Id);
        Assert.Equal(taskSendParams.Message.Parts[0].AsTextPart().Text, result.Message.Parts[0].AsTextPart().Text);
        Assert.Empty(context.Problems);
    }
}