using DomFactory;
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

    internal static RequestDelegate GetAgentCardRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("GetAgentCard", ActivityKind.Server);

            try
            {
                var agentUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path.Value}";
                activity?.AddTag("agent.url", agentUrl);

                var agentCard = taskManager.OnAgentCardQuery(agentUrl);

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(agentCard);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving agent card");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }

    internal static RequestDelegate GetTaskRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("GetTask", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                // Extract optional query parameters
                string? historyLength = context.Request.Query["historyLength"];
                string? metadata = context.Request.Query["metadata"];

                activity?.AddTag("task.id", taskId);

                // Get the task from the task manager
                var agentTask = await taskManager.GetTaskAsync(new TaskQueryParams()
                {
                    Id = taskId,
                    HistoryLength = string.IsNullOrEmpty(historyLength) ? null : int.Parse(historyLength),
                    Metadata = string.IsNullOrEmpty(metadata) ? null : ParsingHelpers.GetMap(JsonDocument.Parse(metadata).RootElement, (ie, ctx) => ie, new ValidationContext("1.0"))
                });

                if (agentTask == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // Return the task
                context.Response.StatusCode = StatusCodes.Status200OK;
                using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                agentTask.Write(writer);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving task");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }

    internal static RequestDelegate CancelTaskRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("CancelTask", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);


                // Cancel the task
                var agentTask = await taskManager.CancelTaskAsync(new TaskIdParams { Id = taskId });
                if (agentTask == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // Return the result
                context.Response.StatusCode = StatusCodes.Status200OK;
                using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                agentTask.Write(writer);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling task");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }

    internal static RequestDelegate SendTaskMessageRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("SendTaskMessage", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);

                // Extract optional query parameters
                string? historyLength = context.Request.Query["historyLength"];

                // Parse request body to get the message
                JsonDocument? requestBody;
                try
                {
                    requestBody = await JsonDocument.ParseAsync(context.Request.Body);
                }
                catch (JsonException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON in request body" });
                    return;
                }

                var validationContext = new ValidationContext("1.0");
                var jsonElement = requestBody.RootElement;

                // Create TaskSendParams object
                var sendParams = TaskSendParams.Load(jsonElement, validationContext);
                sendParams.Id = taskId;
                sendParams.HistoryLength = string.IsNullOrEmpty(historyLength) ? null : int.Parse(historyLength);

                if (validationContext.Problems.Count > 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(validationContext.Problems);
                    return;
                }

                // Send the message to the task
                var result = await taskManager.SendAsync(sendParams);
                if (result == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // Return created status with the task
                context.Response.StatusCode = StatusCodes.Status201Created;
                using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                result.Write(writer);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message to task");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }

    internal static RequestDelegate SendSubscribeTaskRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("SendSubscribeTask", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);

                // Extract optional query parameters
                string? historyLength = context.Request.Query["historyLength"];

                // Parse request body to get the message
                JsonDocument? requestBody;
                try
                {
                    requestBody = await JsonDocument.ParseAsync(context.Request.Body);
                }
                catch (JsonException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON in request body" });
                    return;
                }

                var validationContext = new ValidationContext("1.0");
                var jsonElement = requestBody.RootElement;

                // Create TaskSendParams object
                var sendParams = TaskSendParams.Load(jsonElement, validationContext);
                sendParams.Id = taskId;
                sendParams.HistoryLength = string.IsNullOrEmpty(historyLength) ? null : int.Parse(historyLength);

                if (validationContext.Problems.Count > 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(validationContext.Problems);
                    return;
                }

                // Set response headers for SSE
                context.Response.Headers["Content-Type"] = "text/event-stream";

                var taskEvents = await taskManager.SendSubscribeAsync(sendParams);

                // Process the streaming response
                await foreach (var taskEvent in taskEvents)
                {
                    var stream = new MemoryStream();
                    using var writer = new Utf8JsonWriter(stream);
                    taskEvent.Write(writer);
                    await writer.FlushAsync();
                    writer.Flush();
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"data: {json}\n\n"));
                    await context.Response.BodyWriter.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in send and subscribe");

                // If headers haven't been sent yet, return error
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = ex.Message });
                }
                // Otherwise the connection will just terminate
            }
        };
    }

    internal static RequestDelegate ResubscribeTaskRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("ResubscribeTask", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);

                var taskIdParams = new TaskIdParams { Id = taskId };

                // Set response headers for SSE
                context.Response.Headers["Content-Type"] = "text/event-stream";

                //TODO: Not implemented yet

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in resubscribe");

                // If headers haven't been sent yet, return error
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = ex.Message });
                }
                // Otherwise the connection will just terminate
            }
        };
    }

    internal static RequestDelegate ConfigurePushNotificationRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("ConfigurePushNotification", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);

                // Parse request body to get the push notification config
                JsonDocument? requestBody;
                try
                {
                    requestBody = await JsonDocument.ParseAsync(context.Request.Body);
                }
                catch (JsonException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON in request body" });
                    return;
                }

                var validationContext = new ValidationContext("1.0");
                var jsonElement = requestBody.RootElement;

                // Create notification config
                var pushConfig = new TaskPushNotificationConfig
                {
                    Id = taskId,
                    PushNotificationConfig = PushNotificationConfig.Load(jsonElement, validationContext)
                };

                if (validationContext.Problems.Count > 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(validationContext.Problems);
                    return;
                }

                // Configure push notifications
                var result = await taskManager.SetPushNotificationAsync(pushConfig);
                if (result == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // Return success
                context.Response.StatusCode = StatusCodes.Status200OK;
                using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                result.Write(writer);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring push notifications");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }

    internal static RequestDelegate DeletePushNotificationRequestDelegate(TaskManager taskManager, ILogger logger)
    {
        return async context =>
        {
            using var activity = ActivitySource.StartActivity("DeletePushNotification", ActivityKind.Server);

            try
            {
                // Extract task ID from route
                var taskId = context.Request.RouteValues["id"]?.ToString();
                if (string.IsNullOrEmpty(taskId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Task ID is required" });
                    return;
                }

                activity?.AddTag("task.id", taskId);

                var taskIdParams = new TaskIdParams { Id = taskId };

                // Delete push notification configuration
                var result = await taskManager.SetPushNotificationAsync(new TaskPushNotificationConfig
                {
                    Id = taskId,
                    PushNotificationConfig = new PushNotificationConfig()
                });
                if (result == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // Return success with no content
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting push notifications");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        };
    }
}
