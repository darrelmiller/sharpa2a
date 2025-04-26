using System.Net;
using System.Text;
using System.Text.Json;

namespace A2ATransport;

public class JsonRpcContent : HttpContent
{
    private readonly byte[] _bytes;

    public JsonRpcContent(JsonRpcRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        _bytes = Encoding.UTF8.GetBytes(json);

    }
    public JsonRpcContent(JsonRpcResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        _bytes = Encoding.UTF8.GetBytes(json);
    }

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        stream.Write(_bytes, 0, _bytes.Length);
    }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        SerializeToStream(stream, context, cancellationToken);
        return Task.CompletedTask;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        SerializeToStream(stream, context,new CancellationToken());
        return Task.CompletedTask;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _bytes.Length;
        return true;
    }
}