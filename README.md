# SharpA2A: A .NET implementation of the Google A2A protocol
Interact with agents using the A2A protocol in .NET applications. This library is designed to be used with ASP.NET Core applications and provides a simple way to add A2A support to your agents.

## Library: a2a.AspNetCore
This library adds the MapA2A extension method that allows you to add A2A support to an Agent hosted at the specified path.

```c#
var echoAgent = new EchoAgent();
var echoTaskManager = new TaskManager();
echoAgent.Attach(echoTaskManager);
app.MapA2A(echoTaskManager,"/echo");
```

## Library: a2alib
This library contains the core A2A protocol implementation. It includes the following classes:
- `A2AClient`: Used for making A2A requests to an agent.
- `TaskManager`: Provides standardized support for managing tasks and task execution.
- `ITaskStore`: An interface for abstracting the storage of tasks. `InMemoryTaskStore` is a simple in-memory implementation.

## Library: DomFactory
This library contains helper classes for support deserialization and serialization of A2A messages and JsonRPC envelopes. In theory this can be done with JsonSerializer, but this library makes the process easier to debug and doesn't require an reflection or code generation.

