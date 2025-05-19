using DomFactory;
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


        // Translate Params JsonElement to a concrete type
        var validationContext = new ValidationContext("1.0");
        IJsonRpcParams? parsedParameters = null;
        if (rpcRequest.Params != null)
        {
            var incomingParams = (IJsonRpcIncomingParams)rpcRequest.Params;
            parsedParameters = A2AMethods.ParseParameters(validationContext, rpcRequest.Method, incomingParams.Value);
        }
        // Ensure the request is valid
        if (validationContext.Problems.Count > 0)
        {
            return Results.BadRequest(JsonRpcErrorResponses.InvalidParamsResponse(rpcRequest.Id));
        }

        if (parsedParameters == null)
        {
            return Results.BadRequest(JsonRpcErrorResponses.InvalidParamsResponse(rpcRequest.Id));
        }

        // Dispatch based on return type
        if (A2AMethods.IsStreamingMethod(rpcRequest.Method))
        {
            return await StreamResponse(taskManager, rpcRequest.Id, parsedParameters);
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


    internal static async Task<JsonRpcResponseResult> SingleResponse(TaskManager taskManager, string requestId, string method, IJsonRpcParams? parameters)
    {
        using var activity = ActivitySource.StartActivity($"SingleResponse/{method}", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.method", method);

        JsonRpcResponse? response = null;

        if (parameters == null)
        {
            return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
        }

        switch (method)
        {
            case A2AMethods.TaskSend:
                var agentTask = await taskManager.SendAsync((TaskSendParams)parameters);
                response = JsonRpcResponse.CreateJsonRpcResponse(requestId, agentTask);
                break;
            case A2AMethods.TaskGet:
                var getAgentTask = await taskManager.GetTaskAsync((TaskIdParams)parameters);
                response = JsonRpcResponse.CreateJsonRpcResponse(requestId, getAgentTask);
                break;
            case A2AMethods.TaskCancel:
                var cancelledTask = await taskManager.CancelTaskAsync((TaskIdParams)parameters);
                response = JsonRpcResponse.CreateJsonRpcResponse(requestId, cancelledTask);
                break;
            case A2AMethods.TaskPushNotificationConfigSet:
                var setConfig = await taskManager.SetPushNotificationAsync((TaskPushNotificationConfig)parameters);
                response = JsonRpcResponse.CreateJsonRpcResponse(requestId, setConfig);
                break;
            case A2AMethods.TaskPushNotificationConfigGet:
                var getConfig = await taskManager.GetPushNotificationAsync((TaskIdParams)parameters);
                response = JsonRpcResponse.CreateJsonRpcResponse(requestId, getConfig);
                break;
            default:
                response = JsonRpcErrorResponses.MethodNotFoundResponse(requestId);
                break;
        }

        return new JsonRpcResponseResult(response);
    }
    internal static async Task<IResult> StreamResponse(TaskManager taskManager, string requestId, IJsonRpcParams parameters)
    {
        using var activity = ActivitySource.StartActivity("StreamResponse", ActivityKind.Server);
        activity?.SetTag("request.id", requestId);

        if (parameters == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid parameters");
            return new JsonRpcResponseResult(JsonRpcErrorResponses.InvalidParamsResponse(requestId));
        }

        try
        {
            var taskSendParams = (TaskSendParams)parameters;
            activity?.SetTag("task.id", taskSendParams.Id);
            activity?.SetTag("task.sessionId", taskSendParams.SessionId);

            var taskEvents = await taskManager.SendSubscribeAsync(taskSendParams);

            return new JsonRpcStreamedResult(taskEvents, requestId);

        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new JsonRpcResponseResult(JsonRpcErrorResponses.InternalErrorResponse(requestId, ex.Message));
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
            if (jsonRpcResponse.Error != null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            }

            var writer = new Utf8JsonWriter(httpContext.Response.BodyWriter);
            jsonRpcResponse.Write(writer);
            await writer.FlushAsync();

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
                    Data = new JsonRpcResponse()
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

