using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A2ATransport;
using DomFactory;

namespace A2ALib;

public class A2AClient : IA2AClient
{
    private readonly HttpClient _client;

    public A2AClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<AgentTask> Send(TaskSendParams taskSendParams)
    {
        return await RpcRequest<AgentTask>(taskSendParams, A2AMethods.TaskSend);
    }
    public async Task<AgentTask> GetTask(string taskId) {
        return await RpcRequest<AgentTask>(new TaskIdParams() { Id = taskId }, "task/get");
    }
    public async Task<AgentTask> CancelTask(TaskIdParams taskIdParams) {
        return await RpcRequest<AgentTask>(taskIdParams, "task/cancel");
    }

    public async Task<IAsyncEnumerable<SseItem<TaskUpdateEvent>>> SendSubscribe(TaskSendParams taskSendParams) {
        return null; //TODO
    }
    public async Task<TaskPushNotificationConfig?> SetPushNotification(TaskPushNotificationConfig? pushNotificationConfig) {
        return await RpcRequest<TaskPushNotificationConfig>(pushNotificationConfig, "task/pushNotification/set");
    }
    public async Task<TaskPushNotificationConfig?> GetPushNotification(TaskIdParams taskIdParams) {
        return await RpcRequest<TaskPushNotificationConfig>(taskIdParams!, "task/pushNotification/get");
    }

    private async Task<OUT> RpcRequest<OUT>(IJsonRpcParams jsonRpcParams, string method) where OUT : class
    {
        var request = new JsonRpcRequest()
        {
            Id = Guid.NewGuid().ToString(),
            Method = method,
            Params = jsonRpcParams
        };
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new JsonRpcContent(request)
        });
        response.EnsureSuccessStatusCode();
        var ctx = new ValidationContext("1.0"); //TODO: ValidationContext should be passed from the caller
        return await Parse<OUT>(response.Content,ctx);
    }

    private async Task<T?> Parse<T>(HttpContent content,ValidationContext validationContext) where T: class
    {
        using var stream = await content.ReadAsStreamAsync();
        var jsonDoc = await JsonDocument.ParseAsync(stream);
        var jsonElement = jsonDoc.RootElement;
        var jsonRpcResponse = JsonRpcResponse.Load(jsonElement, validationContext);

        if (jsonRpcResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize response.");
        }
        if (jsonRpcResponse.Result != null)
        {
            _resultHandlers.TryGetValue(typeof(T), out var handler);
            if (handler != null)
            {
                var incomingParams = (IJsonRpcIncomingParams)jsonRpcResponse.Result;
                return (T)handler(incomingParams.Value, validationContext);
            }
            else
            {
                throw new InvalidOperationException($"No handler found for type {typeof(T)}.");
            }
        } else if (jsonRpcResponse.Error != null)
        {
            throw new InvalidOperationException($"Error in response: {jsonRpcResponse.Error.Message} (Code: {jsonRpcResponse.Error.Code})");
        }
        else {
            return null;
        }
    }
    private Dictionary<Type, Func<JsonElement, ValidationContext, object>> _resultHandlers = new()
    {
        { typeof(AgentTask),AgentTask.Load },
        { typeof(TaskUpdateEvent), TaskUpdateEvent.LoadDerived },
        { typeof(TaskPushNotificationConfig), TaskPushNotificationConfig.Load }

    };


}
