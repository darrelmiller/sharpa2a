using System.Text.Json;
namespace SharpA2A.Core;

public interface IJsonRpcOutgoingResult : IJsonRpcResult
{
    void Write(Utf8JsonWriter writer);
}
