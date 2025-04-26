using A2ALib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;


namespace A2ALib;

public static class A2ARouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapA2A(this IEndpointRouteBuilder endpoints, TaskManager taskManager)
    {

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapPost("/", async context =>
        {
            var stream = context.Request.Body;
            var message = await TaskManager.CreateJsonRpcRequestAsync(stream, context.RequestAborted);

            var response = await taskManager.ProcessMessageAsync(message, context.RequestAborted);

            if (response is JsonRpcResponse jsonRpcResponse)
            {
                if (jsonRpcResponse.Error != null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(jsonRpcResponse);
                    return;
                } else {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsJsonAsync(jsonRpcResponse);
                    return;
                }
            }
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        });

        return routeGroup;
    }
}
