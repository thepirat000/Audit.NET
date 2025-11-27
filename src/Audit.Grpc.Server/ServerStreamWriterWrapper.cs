using System.Threading.Tasks;

using Grpc.Core;

namespace Audit.Grpc.Server;

/// <summary>
/// Wrapper for gRPC server stream writer to capture sent messages
/// </summary>
internal class ServerStreamWriterWrapper<T> : IServerStreamWriter<T> where T : class
{
    private readonly IServerStreamWriter<T> _inner;
    private readonly GrpcServerCallAction _action;

    public ServerStreamWriterWrapper(IServerStreamWriter<T> inner, GrpcServerCallAction action)
    {
        _inner = inner;
        _action = action;
        _action.ResponseStream = [];
    }

    public WriteOptions WriteOptions
    {
        get => _inner.WriteOptions!;
        set => _inner.WriteOptions = value;
    }

    public async Task WriteAsync(T message)
    {
        _action.ResponseStream.Add(message);

        await _inner.WriteAsync(message);
    }
}