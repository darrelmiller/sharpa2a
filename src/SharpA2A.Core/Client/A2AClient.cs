using SharpA2A.AspNetCore;
using System.Net.ServerSentEvents;
using System.Text.Json;

namespace SharpA2A.Core;

public class A2AClient : IA2AClient
{
    private readonly HttpClient _client;

    public A2AClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<AgentTask> Send(MessageSendParams taskSendParams)
    {
        return await RpcRequest<MessageSendParams, AgentTask>(taskSendParams, A2AMethods.MessageSend);
    }
    public async Task<AgentTask> GetTask(string taskId)
    {
        return await RpcRequest<TaskIdParams, AgentTask>(new TaskIdParams() { Id = taskId }, A2AMethods.TaskGet);
    }
    public async Task<AgentTask> CancelTask(TaskIdParams taskIdParams)
    {
        return await RpcRequest<TaskIdParams, AgentTask>(taskIdParams, A2AMethods.TaskCancel);
    }

    public async IAsyncEnumerable<SseItem<A2AEvent>> SendSubscribe(MessageSendParams taskSendParams)
    {
        var request = new JsonRpcRequest()
        {
            Id = Guid.NewGuid().ToString(),
            Method = A2AMethods.MessageStream,
            Params = ToJsonElement(taskSendParams)
        };
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new JsonRpcContent(request)
        });
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var sseParser = SseParser.Create<A2AEvent>(stream, (eventType, data) =>
        {
            var reader = new Utf8JsonReader(data);
            var taskEvent = JsonSerializer.Deserialize<A2AEvent>(ref reader, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            if (taskEvent == null)
            {
                throw new InvalidOperationException("Failed to deserialize the event.");
            }
            return taskEvent;
        });
        await foreach (var item in sseParser.EnumerateAsync())
        {
            yield return item;
        }
    }

    public async Task<TaskPushNotificationConfig> SetPushNotification(TaskPushNotificationConfig pushNotificationConfig)
    {
        return await RpcRequest<TaskPushNotificationConfig, TaskPushNotificationConfig>(pushNotificationConfig, "task/pushNotification/set");
    }
    public async Task<TaskPushNotificationConfig> GetPushNotification(TaskIdParams taskIdParams)
    {
        return await RpcRequest<TaskIdParams, TaskPushNotificationConfig>(taskIdParams, "task/pushNotification/get");
    }

    private async Task<OUT> RpcRequest<IN, OUT>(IN jsonRpcParams, string method) where OUT : class
    {
        var request = new JsonRpcRequest()
        {
            Id = Guid.NewGuid().ToString(),
            Method = method,
            Params = ToJsonElement<IN>(jsonRpcParams)
        };
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new JsonRpcContent(request)
        });
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            throw new InvalidOperationException("Invalid content type.");
        }

        // Deserialize the response content directly
        //var responseContent = await response.Content.ReadAsStringAsync();
        var responseStream = await response.Content.ReadAsStreamAsync();
        
        var responseObject = await JsonSerializer.DeserializeAsync<JsonRpcResponse<OUT>>(responseStream, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (responseObject == null)
        {
            throw new InvalidOperationException("Failed to deserialize the response.");
        }

        return responseObject.Result ?? throw new InvalidOperationException("Response does not contain a result.");
    }
    
    public static JsonElement ToJsonElement<T>(T value)
    {
        // TODO: Reduce memory allocations
        // Serialize the object to a JSON string
        var json = JsonSerializer.Serialize(value);

        // Parse the JSON string into a JsonDocument
        using var document = JsonDocument.Parse(json);

        // Return the root element
        return document.RootElement.Clone(); // Clone to avoid disposal issues
    }

}
