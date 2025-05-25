using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharpA2A.Core;
using System.Diagnostics;
using System.Text;
using System.Text.Json;


namespace SharpA2A.AspNetCore;

public class A2AHttpProcessor
{
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.HttpProcessor", "1.0.0");


    internal static Task<IResult> GetAgentCard(TaskManager taskManager, ILogger logger, string agentUrl)
    {
        using var activity = ActivitySource.StartActivity("GetAgentCard", ActivityKind.Server);

        try
        {
            var agentCard = taskManager.OnAgentCardQuery(agentUrl);

            return Task.FromResult(Results.Ok(agentCard));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving agent card");
            return Task.FromResult(Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError));
        }
    }

    internal static async Task<IResult> GetTask(TaskManager taskManager, ILogger logger, string id, int? historyLength, string? metadata)
    {
        using var activity = ActivitySource.StartActivity("GetTask", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {

            var agentTask = await taskManager.GetTaskAsync(new TaskQueryParams()
            {
                Id = id.ToString(),
                HistoryLength = historyLength,
                Metadata = String.IsNullOrWhiteSpace(metadata) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata)    
            });

            if (agentTask == null)
            {
                return Results.NotFound();
            }

            return new AgentTaskResult(agentTask);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving task");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static async Task<IResult> CancelTask(TaskManager taskManager, ILogger logger, string id)
    {
        using var activity = ActivitySource.StartActivity("CancelTask", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            var agentTask = await taskManager.CancelTaskAsync(new TaskIdParams { Id = id });
            if (agentTask == null)
            {
                return Results.NotFound();
            }

            return new AgentTaskResult(agentTask);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling task");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static async Task<IResult> SendTaskMessage(TaskManager taskManager, ILogger logger, string id, MessageSendParams sendParams, int? historyLength, string? metadata)
    {
        using var activity = ActivitySource.StartActivity("SendTaskMessage", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            sendParams.Message.TaskId = id;
            sendParams.Configuration = new MessageSendConfiguration
            {
                HistoryLength = historyLength
            };
            sendParams.Metadata = String.IsNullOrWhiteSpace(metadata) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata);

            var agentTask = await taskManager.SendAsync(sendParams);
            if (agentTask == null)
            {
                return Results.NotFound();
            }

            return new AgentTaskResult(agentTask);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to task");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static async Task<IResult> SendSubscribeTaskMessage(TaskManager taskManager, ILogger logger, string id, MessageSendParams sendParams, int? historyLength, string? metadata)
    {
        using var activity = ActivitySource.StartActivity("SendSubscribeTaskMessage", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            sendParams.Message.TaskId = id;
            sendParams.Configuration = new MessageSendConfiguration()
            {
                HistoryLength = historyLength
            };
            sendParams.Metadata = String.IsNullOrWhiteSpace(metadata) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata);

            var taskEvents = await taskManager.SendSubscribeAsync(sendParams);

            return new TaskEventStreamResult(taskEvents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending subscribe message to task");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static IResult ResubscribeTask(TaskManager taskManager, ILogger logger, string id)
    {
        using var activity = ActivitySource.StartActivity("ResubscribeTask", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            var taskEvents = taskManager.ResubscribeAsync(new TaskIdParams { Id = id });

            return new TaskEventStreamResult(taskEvents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resubscribing to task");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static async Task<IResult> SetPushNotification(TaskManager taskManager, ILogger logger, string id, PushNotificationConfig pushNotificationConfig)
    {
        using var activity = ActivitySource.StartActivity("ConfigurePushNotification", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            var taskIdParams = new TaskIdParams { Id = id };
            var result = await taskManager.SetPushNotificationAsync(new TaskPushNotificationConfig
            {
                Id = id,
                PushNotificationConfig = pushNotificationConfig
            });

            if (result == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring push notification");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal static async Task<IResult> GetPushNotification(TaskManager taskManager, ILogger logger, string id)
    {
        using var activity = ActivitySource.StartActivity("GetPushNotification", ActivityKind.Server);
        activity?.AddTag("task.id", id);

        try
        {
            var taskIdParams = new TaskIdParams { Id = id };
            var result = await taskManager.GetPushNotificationAsync(taskIdParams);

            if (result == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving push notification");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

}


public class AgentTaskResult : IResult
{
    private readonly AgentTask agentTask;

    public AgentTaskResult(AgentTask agentTask)
    {
        this.agentTask = agentTask;
    }
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "application/json";

        return JsonSerializer.SerializeAsync(httpContext.Response.Body, agentTask, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}


public class TaskEventStreamResult : IResult
{
    private readonly IAsyncEnumerable<TaskUpdateEvent> taskEvents;

    public TaskEventStreamResult(IAsyncEnumerable<TaskUpdateEvent> taskEvents)
    {
        this.taskEvents = taskEvents;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/event-stream";
        await foreach (var taskEvent in taskEvents)
        {
            var json = JsonSerializer.Serialize(taskEvent);
            await httpContext.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"data: {json}\n\n"));
            await httpContext.Response.BodyWriter.FlushAsync();
        }
    }
}