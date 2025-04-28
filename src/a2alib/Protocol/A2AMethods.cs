using System.Text.Json;
using DomFactory;

namespace A2ALib;

public static class A2AMethods
{
    public const string TaskSend = "task/send";
    public const string TaskGet = "task/get";
    public const string TaskCancel = "task/cancel";
    public const string TaskPushNotificationConfigSet = "task/pushnotificationconfig/set";
    public const string TaskPushNotificationConfigGet = "task/pushnotificationconfig/get";
    public const string TaskSendSubscribe = "task/sendsubscribe";

    public static bool IsStreamingMethod(string method)
    {
        return method == TaskSendSubscribe;
    }

    public static IJsonRpcParams ParseParameters(ValidationContext context, string method, JsonElement paramsElement)
    {
        object parsedParams;
        switch (method)
        {
            case A2AMethods.TaskSend:
                parsedParams = TaskSendParams.Load(paramsElement, context);
                break;
            case A2AMethods.TaskGet:
                parsedParams = TaskIdParams.Load(paramsElement, context);
                break;

            case A2AMethods.TaskCancel:
                parsedParams = TaskIdParams.Load(paramsElement, context);
                break;

            case A2AMethods.TaskPushNotificationConfigSet:
                parsedParams = TaskPushNotificationConfig.Load(paramsElement, context);
                break;

            case A2AMethods.TaskPushNotificationConfigGet:
                parsedParams = TaskIdParams.Load(paramsElement, context);
                break;

            case A2AMethods.TaskSendSubscribe:
                parsedParams = TaskSendParams.Load(paramsElement, context);
                break;

            default:
                throw new NotImplementedException($"Method {method} not implemented.");
        }
        return (IJsonRpcParams)parsedParams;
    }

}
