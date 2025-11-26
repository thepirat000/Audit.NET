using Audit.Core;
using Audit.Core.Extensions;

using Grpc.Core;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Grpc.Client;

/// <summary>
/// Wrapper for gRPC server stream reader to capture received messages
/// </summary>
internal class ServerStreamWriterWrapper<T> : IAsyncStreamReader<T> where T : class
{
    private readonly IAsyncStreamReader<T> _inner;
    private readonly Task<IAuditScope> _auditScopeCreationTask;
    private readonly bool _includeResponse;
    private IAuditScope _auditScope = null;

    public ServerStreamWriterWrapper(IAsyncStreamReader<T> inner, Task<IAuditScope> auditScopeCreationTask, bool includeResponse)
    {
        _inner = inner;
        _auditScopeCreationTask = auditScopeCreationTask;
        _includeResponse = includeResponse;
    }

    public T Current => _inner.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        _auditScope ??= await _auditScopeCreationTask;

        var action = _auditScope.EventAs<AuditEventGrpcClient>().Action;

        if (_includeResponse)
        {
            action.ResponseStream ??= [];
        }

        bool hasNext;
        
        try
        {
            hasNext = await _inner.MoveNext(cancellationToken);
        }
        catch (Exception ex)
        {
            action.Exception = ex.GetExceptionInfo();
            action.IsSuccess = false;

            throw;
        }
        
        if (hasNext && _includeResponse)
        {
            action.ResponseStream.Add(_inner.Current);
        }

        return hasNext;
    }
}