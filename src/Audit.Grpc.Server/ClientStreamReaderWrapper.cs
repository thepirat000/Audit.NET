using System;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Extensions;
using Audit.Grpc.Server;
using Grpc.Core;

namespace Audit.Grpc.Server;

/// <summary>
/// Wrapper for gRPC client stream reader to capture received messages on the server
/// </summary>
internal class ClientStreamReaderWrapper<T> : IAsyncStreamReader<T> where T : class
{
    private readonly IAsyncStreamReader<T> _inner;
    private readonly GrpcServerCallAction _action;

    public ClientStreamReaderWrapper(IAsyncStreamReader<T> inner, GrpcServerCallAction action)
    {
        _inner = inner;
        _action = action;
        _action.RequestStream = [];
    }

    public T Current => _inner.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        bool hasNext;

        try
        {
            hasNext = await _inner.MoveNext(cancellationToken);
        }
        catch (Exception ex)
        {
            _action.Exception = ex.GetExceptionInfo();
            _action.IsSuccess = false;

            throw;
        }

        if (hasNext)
        {
            _action.RequestStream.Add(_inner.Current);
        }

        return hasNext;
    }
}