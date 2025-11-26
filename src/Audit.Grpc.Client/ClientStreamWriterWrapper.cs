using System.Threading.Tasks;
using Grpc.Core;

namespace Audit.Grpc.Client;

/// <summary>
/// Wrapper for gRPC client stream writer to capture sent messages
/// </summary>
internal class ClientStreamWriterWrapper<T> : IClientStreamWriter<T> where T : class
{
    private readonly IClientStreamWriter<T> _inner;
    private readonly GrpcClientCallAction _call;

    public ClientStreamWriterWrapper(IClientStreamWriter<T> inner, GrpcClientCallAction call)
    {
        _inner = inner;
        _call = call;
        _call.RequestStream = [];
    }

    public WriteOptions WriteOptions
    {
        get => _inner.WriteOptions!;
        set => _inner.WriteOptions = value;
    }

    public async Task WriteAsync(T message)
    {
        _call.RequestStream.Add(message);

        await _inner.WriteAsync(message);
    }

    public Task CompleteAsync()
    {
        return _inner.CompleteAsync();
    }
}