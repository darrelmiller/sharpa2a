using Microsoft.AspNetCore.Http;
using SharpA2A.Core;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace SharpA2A.AspNetCore;

public static class A2AJsonRpcProcessor
{
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.Processor", "1.0.0");

    internal static async Task<IResult> ProcessRequest(TaskManager taskManager, JsonRpcRequest rpcRequest)
    {
        using var activity = ActivitySource.StartActivity("HandleA2ARequest", ActivityKind.Server);
        activity?.AddTag("request.id", rpcRequest.Id);
        activity?.AddTag("request.method", rpcRequest.Method);

        var parsedParameters = rpcRequest.Params;
        // Dispatch based on return type
        if (A2AMethods.IsStreamingMethod(rpcRequest.Method))
        {
            return await StreamResponse(taskManager, rpcRequest.Id,rpcRequest.Method, parsedParameters);
        }
        else
        {
            try
            {
                return await SingleResponse(taskManager, rpcRequest.Id, rpcRequest.Method, parsedParameters); ;
            }
            catch (Exception e)
            {
                return new JsonRpcResponseResult(JsonRpcErrorResponses.InternalErrorResponse(rpcRequest.Id, e.Message));
            }
        }
    }


    internal static async Task<JsonRpcResponseResult> SingleResponse(TaskManager taskManager, string requestId, string method, JsonElement? parameters)
    {
        using var activity = ActivitySource.StartActivity($"SingleResponse/{method}", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.method", method);

        JsonRpcResponse? response = null;

        if (parameters == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
            return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
        }

        switch (method)
        {
            case A2AMethods.MessageSend:
                var taskSendParams = JsonSerializer.Deserialize<MessageSendParams>(parameters.Value.GetRawText()); //TODO stop the double parsing
                if (taskSendParams == null)
                {
                    response = JsonRpcErrorResponses.InvalidParamsResponse(requestId);
                    break;
                }
                var agentTask = await taskManager.SendAsync(taskSendParams);
                response = JsonRpcResponse<AgentTask?>.CreateJsonRpcResponse(requestId, agentTask);
                break;
            case A2AMethods.TaskGet:
                var taskIdParams = JsonSerializer.Deserialize<TaskQueryParams>(parameters.Value.GetRawText());
                if (taskIdParams == null)
                {
                    response = JsonRpcErrorResponses.InvalidParamsResponse(requestId);
                    break;
                }
                var getAgentTask = await taskManager.GetTaskAsync(taskIdParams);
                response = JsonRpcResponse<AgentTask?>.CreateJsonRpcResponse(requestId, getAgentTask);
                break;
            case A2AMethods.TaskCancel:
                var taskIdParamsCancel = JsonSerializer.Deserialize<TaskIdParams>(parameters.Value.GetRawText());
                if (taskIdParamsCancel == null)
                {
                    response = JsonRpcErrorResponses.InvalidParamsResponse(requestId);
                    break;
                }
                var cancelledTask = await taskManager.CancelTaskAsync(taskIdParamsCancel);
                response = JsonRpcResponse<AgentTask?>.CreateJsonRpcResponse(requestId, cancelledTask);
                break;
            case A2AMethods.TaskPushNotificationConfigSet:
                var taskPushNotificationConfig = JsonSerializer.Deserialize<TaskPushNotificationConfig>(parameters.Value.GetRawText());
                if (taskPushNotificationConfig == null)
                {
                    response = JsonRpcErrorResponses.InvalidParamsResponse(requestId);
                    break;
                }
                var setConfig = await taskManager.SetPushNotificationAsync(taskPushNotificationConfig);
                response = JsonRpcResponse<TaskPushNotificationConfig?>.CreateJsonRpcResponse(requestId, setConfig);
                break;
            case A2AMethods.TaskPushNotificationConfigGet:
                var taskIdParamsGetConfig = JsonSerializer.Deserialize<TaskIdParams>(parameters.Value.GetRawText());
                if (taskIdParamsGetConfig == null)
                {
                    response = JsonRpcErrorResponses.InvalidParamsResponse(requestId);
                    break;
                }
                var getConfig = await taskManager.GetPushNotificationAsync(taskIdParamsGetConfig);
                response = JsonRpcResponse<TaskPushNotificationConfig?>.CreateJsonRpcResponse(requestId, getConfig);
                break;
            default:
                response = JsonRpcErrorResponses.MethodNotFoundResponse(requestId);
                break;
        }

        return new JsonRpcResponseResult(response);
    }
    internal static async Task<IResult> StreamResponse(TaskManager taskManager, string requestId, string method, JsonElement? parameters)
    {
        using var activity = ActivitySource.StartActivity("StreamResponse", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);

        if (parameters == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
            return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
        }
        switch (method)
        {
            case A2AMethods.TaskResubscribe:
                var taskIdParams = JsonSerializer.Deserialize<TaskIdParams>(parameters.Value.GetRawText());
                if (taskIdParams == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
                    return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
                }
                var taskEvents = taskManager.ResubscribeAsync(taskIdParams);
                return new JsonRpcStreamedResult(taskEvents, requestId);
            case A2AMethods.MessageStream:
                try
                {
                    var taskSendParams = JsonSerializer.Deserialize<MessageSendParams>(parameters.Value.GetRawText());
                    if (taskSendParams == null)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
                        return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
                    }

                    var sendEvents = await taskManager.SendSubscribeAsync(taskSendParams);

                    return new JsonRpcStreamedResult(sendEvents, requestId);

                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    return new JsonRpcResponseResult(JsonRpcErrorResponses.InternalErrorResponse(requestId, ex.Message));
                }
            default:
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid method");
                return new JsonRpcResponseResult(JsonRpcErrorResponses.MethodNotFoundResponse(requestId));
        }
    }
}


    public class JsonRpcResponseResult : IResult
    {
        private readonly JsonRpcResponse jsonRpcResponse;

        public JsonRpcResponseResult(JsonRpcResponse jsonRpcResponse)
        {
            this.jsonRpcResponse = jsonRpcResponse;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.ContentType = "application/json";
            if (jsonRpcResponse is JsonRpcErrorResponse)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            }

            await JsonSerializer.SerializeAsync(httpContext.Response.Body,  jsonRpcResponse, jsonRpcResponse.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
    }

    public class JsonRpcStreamedResult : IResult {
        private readonly IAsyncEnumerable<TaskUpdateEvent> _events;
        private readonly string requestId;

        public JsonRpcStreamedResult(IAsyncEnumerable<TaskUpdateEvent> events, string requestId)
        {
            _events = events;
            this.requestId = requestId;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/event-stream";

            await foreach (var taskEvent in _events)
            {
                var sseItem = new A2ASseItem()
                {
                    Data = new JsonRpcResponse<A2AResponse>()
                    {
                        Id = requestId,
                        Result = taskEvent,
                        JsonRpc = "2.0"
                    }
                };
                await sseItem.WriteAsync(httpContext.Response.BodyWriter);
            }
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
                await JsonSerializer.SerializeAsync(jsonStream, Data, Data.GetType(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
                jsonStream.Position = 0;
                using var reader = new StreamReader(jsonStream);
                var json = reader.ReadToEnd();
                await writer.WriteAsync(Encoding.UTF8.GetBytes($"data: {json}\n\n"));
                await writer.FlushAsync();
            }
        }
    }

