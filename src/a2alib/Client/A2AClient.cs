using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A2ATransport;

namespace A2ALib;

public class A2AClient : IA2AClient
{
    private readonly HttpClient _client;

    public A2AClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<AgentTask?> Send(TaskSendParams taskSendParams)
    {
        return await RpcRequest<TaskSendParams,AgentTask?>(taskSendParams, A2AMethods.TaskSend);
    }
    public async Task<AgentTask?> GetTask(string taskId) {
        return await RpcRequest<TaskSendParams,AgentTask?>(new TaskSendParams() { Id = taskId }, "task/get");
    }
    public async Task<AgentTask?> CancelTask(TaskIdParams taskIdParams) {
        return await RpcRequest<TaskIdParams,AgentTask?>(taskIdParams, "task/cancel");
    }
    //TODO: This signature is not correct usage of SseItem
    public async Task<SseItem<TaskUpdateEvent>> SendSubscribe(TaskSendParams taskSendParams) {
        return await RpcRequest<TaskSendParams,SseItem<TaskUpdateEvent>>(taskSendParams, "task/subscribe");
    }
    public async Task<TaskPushNotificationConfig?> SetPushNotification(TaskPushNotificationConfig? pushNotificationConfig) {
        return await RpcRequest<TaskPushNotificationConfig?,TaskPushNotificationConfig?>(pushNotificationConfig, "task/pushNotification/set");
    }
    public async Task<TaskPushNotificationConfig?> GetPushNotification(TaskIdParams? taskIdParams) {
        return await RpcRequest<TaskIdParams,TaskPushNotificationConfig?>(taskIdParams!, "task/pushNotification/get");
    }

    private async Task<OUT> RpcRequest<IN,OUT>(IN taskSendParams, string v)
    {
        var request = new JsonRpcRequest()
        {
            Id = Guid.NewGuid().ToString(),
            Method = v,
            Params = taskSendParams
        };
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new JsonRpcContent(request)
        });
        response.EnsureSuccessStatusCode();

        return await Parse<OUT>(response.Content);
    }

    private async Task<T> Parse<T>(HttpContent content)
    {
        using var stream = await content.ReadAsStreamAsync();
        var jsonRpcResponse = JsonSerializer.Deserialize<JsonRpcResponse>(stream);

        if (jsonRpcResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize response.");
        }
        if (jsonRpcResponse.Result is JsonElement jsonElement)
        {
            var json = jsonElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize response.");
        }
        else
        {
            throw new InvalidOperationException("Invalid response format.");
        }
    }


}
