using A2ALib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace A2ATransport;


public static class A2ARouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapA2A(this IEndpointRouteBuilder endpoints, TaskManager taskManager)
    {

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapPost("/", requestDelegate: async context =>
        {
            var stream = context.Request.Body;
            var rpcRequest = await A2AProcessor.CreateJsonRpcRequestAsync(stream, context.RequestAborted);

            if (rpcRequest.Method == A2AMethods.TaskSendSubscribe)
            {
                await A2AProcessor.StreamResponse(taskManager, context, rpcRequest);
            }
            else
            {
                await A2AProcessor.SingleResponse(taskManager, context, rpcRequest);
            }
        });

        return routeGroup;
    }
}
