using ModelContextProtocol.Protocol.Messages;
using A2ALib;

namespace A2ATransport;

public static class McpEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapA2A(this IEndpointRouteBuilder endpoints, TaskManager taskManager)
    {

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapPost("/", async context =>
        {
            var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(context.RequestAborted);
            if (message is not JsonRpcRequest)
            {
                await Results.BadRequest("No message in request body.").ExecuteAsync(context);
                return;
            }

            var response = await taskManager.ProcessMessageAsync(message as JsonRpcRequest, context.RequestAborted);
            if (response is JsonRpcError errorResponse)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }
            if (response is JsonRpcResponse jsonRpcResponse)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(jsonRpcResponse);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        });

        return routeGroup;
    }
}
