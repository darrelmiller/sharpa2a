using System.Text.Json;
namespace SharpA2A.Core;

public interface IJsonRpcIncomingParams : IJsonRpcParams
{
    JsonElement Value { get; }
}
