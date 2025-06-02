using System.Text.Json;

namespace SharpA2A.Core;

public static class A2AMethods
{
    public const string MessageSend = "message/send";
    public const string MessageStream = "message/stream";
    public const string TaskGet = "task/get";
    public const string TaskCancel = "task/cancel";
    public const string TaskResubscribe = "task/resubscribe";
    public const string TaskPushNotificationConfigSet = "task/pushnotificationconfig/set";
    public const string TaskPushNotificationConfigGet = "task/pushnotificationconfig/get";
    

    public static bool IsStreamingMethod(string method)
    {
        return method == MessageStream || method == TaskResubscribe;
    }
}

