using System.Text.Json;
namespace SharpA2A.Core;

public interface IJsonRpcOutgoingParams : IJsonRpcParams
{
    void Write(Utf8JsonWriter writer);
}
