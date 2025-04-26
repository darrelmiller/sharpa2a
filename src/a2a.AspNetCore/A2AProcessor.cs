using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using A2ALib;
using Microsoft.AspNetCore.Http;

namespace A2ATransport;

public static class A2AProcessor
{

    internal static async Task SingleResponse(TaskManager taskManager, HttpContext context, JsonRpcRequest message)
    {
        JsonRpcResponse? response = null;

        switch (message.Method)
        {
            case A2AMethods.TaskSend:
                var agentTask = await taskManager.SendAsync(message.Params as TaskSendParams);
                response = CreateJsonRpcResponse(message, agentTask);
                break;
            case A2AMethods.TaskGet:
                var getAgentTask = await taskManager.GetTaskAsync(message.Params as TaskIdParams);
                response = CreateJsonRpcResponse(message, getAgentTask);
                break;
            case A2AMethods.TaskCancel:
                var cancelledTask = await taskManager.CancelTaskAsync(message.Params as TaskIdParams);
                response = CreateJsonRpcResponse(message, cancelledTask);
                break;
            case A2AMethods.TaskPushNotificationConfigSet:
                var setConfig = await taskManager.SetPushNotificationAsync(message.Params as TaskPushNotificationConfig);
                response = CreateJsonRpcResponse(message, setConfig);
                break;
            case A2AMethods.TaskPushNotificationConfigGet:
                var getConfig = await taskManager.GetPushNotificationAsync(message.Params as TaskIdParams);
                response = CreateJsonRpcResponse(message, getConfig);
                break;
            case A2AMethods.TaskSendSubscribe:
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
        }

        if (response is JsonRpcResponse jsonRpcResponse)
        {
            context.Response.ContentType = "application/json";
            if (jsonRpcResponse.Error != null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(jsonRpcResponse);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(jsonRpcResponse);
            }
        }
    }

    internal static async Task StreamResponse(TaskManager taskManager, HttpContext context, JsonRpcRequest message)
    {
        var taskEvents = await taskManager.SendSubscribeAsync(message.Params as TaskSendParams);
        context.Response.ContentType = "text/event-stream";
        context.Response.StatusCode = StatusCodes.Status200OK;

        await foreach (var taskEvent in taskEvents)
        {
            await context.Response.WriteAsJsonAsync(taskEvent);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }
        await context.Response.CompleteAsync();
    }

    internal static async Task<JsonRpcRequest> CreateJsonRpcRequestAsync(Stream stream, CancellationToken requestAborted)
    {

            var jsonNode = await JsonNode.ParseAsync(stream);
            var message = JsonRpcRequest.Load(jsonNode);
            switch (message.Method)
            {
                case A2AMethods.TaskSend:
                    message.Params = JsonSerializer.Deserialize<TaskSendParams>((JsonNode)message.Params);
                    break;
                case A2AMethods.TaskGet:
                    message.Params = JsonSerializer.Deserialize<TaskIdParams>((JsonNode)message.Params);
                    break;
                case A2AMethods.TaskCancel:
                    message.Params = JsonSerializer.Deserialize<TaskIdParams>((JsonNode)message.Params);
                    break;
                case A2AMethods.TaskPushNotificationConfigSet:
                    message.Params = JsonSerializer.Deserialize<TaskPushNotificationConfig>((JsonNode)message.Params);
                    break;
                case A2AMethods.TaskPushNotificationConfigGet:
                    message.Params = JsonSerializer.Deserialize<TaskIdParams>((JsonNode)message.Params);
                    break;
                case A2AMethods.TaskSendSubscribe:
                    message.Params = JsonSerializer.Deserialize<TaskSendParams>((JsonNode)message.Params);
                    break;
                default:
                    throw new NotImplementedException($"Method {message.Method} not implemented.");
            }
            return message;
    }

    private static JsonRpcResponse CreateJsonRpcResponse<T>(JsonRpcRequest message, T result)
    {
        return new JsonRpcResponse()
        {
            Id = message.Id,
            Result = result,
            JsonRpc = "2.0"
        };
    }

}