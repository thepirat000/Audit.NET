using System.Threading.Tasks;
using Grpc.Core;

namespace Audit.Grpc.Client;

/// <summary>
/// Wrapper for gRPC client stream writer to capture sent messages
/// </summary>
internal class ClientStreamWriterWrapper<T> : IClientStreamWriter<T> where T : class
{
    private readonly IClientStreamWriter<T> _inner;
    private readonly GrpcClientCallAction _action;

    public ClientStreamWriterWrapper(IClientStreamWriter<T> inner, GrpcClientCallAction action)
    {
        _inner = inner;
        _action = action;
        _action.RequestStream = [];
    }

    public WriteOptions WriteOptions
    {
        get => _inner.WriteOptions!;
        set => _inner.WriteOptions = value;
    }

    public async Task WriteAsync(T message)
    {
        _action.RequestStream.Add(message);

        await _inner.WriteAsync(message);
    }

    public Task CompleteAsync()
    {
        return _inner.CompleteAsync();
    }
}