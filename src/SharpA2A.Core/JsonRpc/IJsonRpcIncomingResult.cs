using System.Text.Json;
namespace SharpA2A.Core;

public interface IJsonRpcIncomingResult : IJsonRpcResult
{
    JsonElement Value { get; }
}
