using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using A2ALib;
using DomFactory;
using Microsoft.AspNetCore.Http;

namespace A2ATransport;

public static class A2AProcessor
{
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.Processor", "1.0.0"); internal static async Task<JsonRpcResponse> SingleResponse(TaskManager taskManager, HttpContext context, string requestId, string method, IJsonRpcParams? parameters)
    {
        using var activity = ActivitySource.StartActivity($"SingleResponse/{method}", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.method", method);

        JsonRpcResponse? response = null;

        if (parameters == null)
        {
            response = new JsonRpcResponse()
            {
                Id = requestId,
                Error = new JsonRpcError()
                {
                    Code = -32602,
                    Message = "Invalid params"
                },
                JsonRpc = "2.0"
            };
            return response;
        }

        switch (method)
        {
            case A2AMethods.TaskSend:

                var agentTask = await taskManager.SendAsync((TaskSendParams)parameters);
                response = CreateJsonRpcResponse(requestId, agentTask);
                break;
            case A2AMethods.TaskGet:
                var getAgentTask = await taskManager.GetTaskAsync((TaskIdParams)parameters);
                response = CreateJsonRpcResponse(requestId, getAgentTask);
                break;
            case A2AMethods.TaskCancel:
                var cancelledTask = await taskManager.CancelTaskAsync((TaskIdParams)parameters);
                response = CreateJsonRpcResponse(requestId, cancelledTask);
                break;
            case A2AMethods.TaskPushNotificationConfigSet:
                var setConfig = await taskManager.SetPushNotificationAsync((TaskPushNotificationConfig)parameters);
                response = CreateJsonRpcResponse(requestId, setConfig);
                break;
            case A2AMethods.TaskPushNotificationConfigGet:
                var getConfig = await taskManager.GetPushNotificationAsync((TaskIdParams)parameters);
                response = CreateJsonRpcResponse(requestId, getConfig);
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response = new JsonRpcResponse()
                {
                    Id = requestId,
                    Error = new JsonRpcError()
                    {
                        Code = -32601,
                        Message = "Method not found"
                    },
                    JsonRpc = "2.0"
                };
                break;
        }

        return response;
    }
    internal static async Task StreamResponse(TaskManager taskManager, HttpContext context, string requestId, IJsonRpcParams parameters)
    {
        using var activity = ActivitySource.StartActivity("StreamResponse", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);

        if (parameters == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = new JsonRpcResponse()
            {
                Id = requestId,
                Error = new JsonRpcError()
                {
                    Code = -32602,
                    Message = "Invalid params"
                },
                JsonRpc = "2.0"
            };
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
            A2ARouteBuilderExtensions.WriteJsonRpcResponse(context, response);
            return;
        }

        try
        {
            var taskSendParams = (TaskSendParams)parameters;
            activity?.SetTag("task.id", taskSendParams.Id);
            activity?.SetTag("task.sessionId", taskSendParams.SessionId);
            
            var taskEvents = await taskManager.SendSubscribeAsync(taskSendParams);
            context.Response.ContentType = "text/event-stream";
            context.Response.StatusCode = StatusCodes.Status200OK;

            int eventCount = 0;
            await foreach (var taskEvent in taskEvents)
            {
                var sseItem = new A2ASseItem()
                {
                    Data = new JsonRpcResponse()
                    {
                        Id = requestId,
                        Result = taskEvent,
                        JsonRpc = "2.0"
                    }
                };
                  // Capture information about each event
                activity?.AddEvent(new ActivityEvent($"Stream event {eventCount++}"));
                    
                await sseItem.WriteAsync(context.Response.BodyWriter);
                await context.Response.BodyWriter.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var response = new JsonRpcResponse()
                {
                    Id = requestId,
                    Error = new JsonRpcError()
                    {
                        Code = -32000,
                        Message = "Server error"
                    },
                    JsonRpc = "2.0"
                };
                A2ARouteBuilderExtensions.WriteJsonRpcResponse(context, response);
            }
        }
    }
    internal static async Task<JsonRpcRequest> ParseJsonRpcRequestAsync(ValidationContext validationContext, Stream stream, CancellationToken requestAborted)
    {
        using var activity = ActivitySource.StartActivity("ParseJsonRpcRequest", ActivityKind.Internal);

        try
        {
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: requestAborted);
            var message = JsonRpcRequest.Load(doc.RootElement, validationContext);

            if (message != null)
            {
                activity?.SetTag("request.id", message.Id);
                activity?.SetTag("request.method", message.Method);
            }

            return message ?? throw new InvalidOperationException("Failed to parse JSON-RPC request");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }


    private static JsonRpcResponse CreateJsonRpcResponse<T>(string requestId, T result) where T : IJsonRpcResult?
    {
        return new JsonRpcResponse()
        {
            Id = requestId,
            Result = result,
            JsonRpc = "2.0"
        };
    }

}

public class A2ASseItem
{
    public JsonRpcResponse? Data { get; set; }

    public async Task WriteAsync(PipeWriter writer)
    {

        if (Data != null)
        {
            var jsonStream = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(jsonStream, new JsonWriterOptions { Indented = false });
            Data.Write(jsonWriter);
            jsonWriter.Flush();
            jsonStream.Position = 0;
            using var reader = new StreamReader(jsonStream);
            var json = reader.ReadToEnd();
            await writer.WriteAsync(Encoding.UTF8.GetBytes($"data: {json}\n\n"));
            await writer.FlushAsync();
        }
    }
}

