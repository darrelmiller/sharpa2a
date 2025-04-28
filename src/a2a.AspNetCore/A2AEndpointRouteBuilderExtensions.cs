using A2ALib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DomFactory;
using Microsoft.AspNetCore.Http;
using System.Text.Json;


namespace A2ATransport;


public static class A2ARouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapA2A(this IEndpointRouteBuilder endpoints, TaskManager taskManager, string path)
    {

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapPost(path, requestDelegate: async context =>
        {
            var validationContext = new ValidationContext("1.0");
            // Parse generic JSON-RPC request
            var rpcRequest = await A2AProcessor.ParseJsonRpcRequestAsync(validationContext,context.Request.Body, context.RequestAborted);

            // Translate Params JsonElement to a concrete type
            IJsonRpcParams? parsedParameters = null;
            if (rpcRequest.Params != null) {
                var incomingParams = (IJsonRpcIncomingParams)rpcRequest.Params;
                parsedParameters = A2AMethods.ParseParameters(validationContext, rpcRequest.Method, incomingParams.Value);
            }
            // Ensure the request is valid
            if (validationContext.Problems.Count > 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(validationContext.Problems);
                return;
            }

            // Dispatch based on return type
            if (A2AMethods.IsStreamingMethod(rpcRequest.Method))
            {
                await A2AProcessor.StreamResponse(taskManager, context, rpcRequest);
            }
            else
            {
                try {
 
                    var jsonRpcResponse = await A2AProcessor.SingleResponse(taskManager, context, rpcRequest.Id, rpcRequest.Method, parsedParameters);

                    context.Response.ContentType = "application/json";
                    if (jsonRpcResponse.Error != null)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                        jsonRpcResponse.Write(writer);
                        writer.Flush();
                        await context.Response.CompleteAsync();
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                        jsonRpcResponse.Write(writer);
                        writer.Flush();
                        await context.Response.CompleteAsync();
                    }
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    var jsonRpcResponse = new JsonRpcResponse()
                    {
                        Id = rpcRequest.Id,
                        Error = new JsonRpcError()
                        {
                            Code = -32603,
                            Message = e.Message
                        },
                        JsonRpc = "2.0"
                    };
                    var writer = new Utf8JsonWriter(context.Response.BodyWriter);
                    jsonRpcResponse.Write(writer);
                    writer.Flush();
                    await context.Response.CompleteAsync();

                }
            }
        });

        return routeGroup;
    }
}
