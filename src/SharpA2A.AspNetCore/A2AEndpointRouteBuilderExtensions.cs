using DomFactory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpA2A.Core;
using System.Diagnostics;
using System.Text.Json;

namespace SharpA2A.AspNetCore;

public static class A2ARouteBuilderExtensions
{
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.Endpoint", "1.0.0");

    public static IEndpointConventionBuilder MapA2A(this IEndpointRouteBuilder endpoints, ITaskManager taskManager, string path)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<IEndpointRouteBuilder>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapGet($"{path}/.well-known/agent.json", async context =>
        {
            var agentUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
            var agentCard = taskManager.OnAgentCardQuery(agentUrl);
            await context.Response.WriteAsJsonAsync(agentCard);
        });

        routeGroup.MapPost(path, requestDelegate: async context =>
        {
            using var activity = ActivitySource.StartActivity("HandleA2ARequest", ActivityKind.Server);
            activity?.AddTag("endpoint.path", path);

            var validationContext = new ValidationContext("1.0");
            // Parse generic JSON-RPC request
            var rpcRequest = await A2AProcessor.ParseJsonRpcRequestAsync(validationContext, context.Request.Body, context.RequestAborted);

            // Translate Params JsonElement to a concrete type
            IJsonRpcParams? parsedParameters = null;
            if (rpcRequest.Params != null)
            {
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
                if (parsedParameters == null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    var errorResponse = new JsonRpcResponse()
                    {
                        Id = rpcRequest.Id,
                        Error = new JsonRpcError()
                        {
                            Code = -32602,
                            Message = "Invalid params"
                        },
                        JsonRpc = "2.0"
                    };
                    WriteJsonRpcResponse(context, errorResponse);
                    return;
                }
                await A2AProcessor.StreamResponse(taskManager, context, rpcRequest.Id, parsedParameters);
                await context.Response.CompleteAsync();
            }
            else
            {
                try
                {
                    activity?.AddTag("request.id", rpcRequest.Id);
                    activity?.AddTag("request.method", rpcRequest.Method);

                    var jsonRpcResponse = await A2AProcessor.SingleResponse(taskManager, context, rpcRequest.Id, rpcRequest.Method, parsedParameters);

                    context.Response.ContentType = "application/json";
                    if (jsonRpcResponse.Error != null)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        WriteJsonRpcResponse(context, jsonRpcResponse);
                        await context.Response.CompleteAsync();
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        WriteJsonRpcResponse(context, jsonRpcResponse);
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
                    WriteJsonRpcResponse(context, jsonRpcResponse);
                    await context.Response.CompleteAsync();

                }
            }
        });

        return routeGroup;
    }

    internal static void WriteJsonRpcResponse(HttpContext context, JsonRpcResponse jsonRpcResponse)
    {
        var writer = new Utf8JsonWriter(context.Response.BodyWriter);
        jsonRpcResponse.Write(writer);
        writer.Flush();
    }
}
