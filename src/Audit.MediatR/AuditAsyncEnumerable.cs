using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;

namespace Audit.MediatR;

/// <summary>
/// <see cref="IAsyncEnumerable{T}"/> wrapper that audits each item yielded by the inner enumerable.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AuditAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly IAsyncEnumerable<T> _inner;
    private readonly Action<T> _onNewItemAction;
    private readonly Action<Exception> _onExceptionAction;
    private readonly Task<IAuditScope> _auditScopeCreateTask;

    public AuditAsyncEnumerable(IAsyncEnumerable<T> inner, Task<IAuditScope> auditScopeCreateTask, Action<T> onNewItemAction, Action<Exception> onExceptionAction)
    {
        _inner = inner;
        _auditScopeCreateTask = auditScopeCreateTask;
        _onNewItemAction = onNewItemAction;
        _onExceptionAction = onExceptionAction;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new AuditAsyncEnumerator(_inner.GetAsyncEnumerator(cancellationToken), _onNewItemAction, _onExceptionAction, _auditScopeCreateTask);
    }
    
    private sealed class AuditAsyncEnumerator : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _innerEnum;
        private readonly Action<T> _onNewItemAction;
        private readonly Action<Exception> _onExceptionAction;
        private readonly Task<IAuditScope> _auditScopeCreateTask;
        private IAuditScope _auditScope = null;
        
        public AuditAsyncEnumerator(IAsyncEnumerator<T> innerEnum, Action<T> onNewItemAction, Action<Exception> onExceptionAction, Task<IAuditScope> auditScopeCreateTask)
        {
            _innerEnum = innerEnum;
            _onNewItemAction = onNewItemAction;
            _onExceptionAction = onExceptionAction;
            _auditScopeCreateTask = auditScopeCreateTask;
        }

        public T Current => _innerEnum.Current;

        public async ValueTask<bool> MoveNextAsync()
        {
            _auditScope ??= await _auditScopeCreateTask;

            bool hasNext;
            try
            {
                hasNext = await _innerEnum.MoveNextAsync();
            }
            catch (Exception ex)
            {
                _onExceptionAction?.Invoke(ex);

                throw;
            }

            if (hasNext)
            {
                // Log the item
                var item = _innerEnum.Current;

                _onNewItemAction?.Invoke(item);
            }
            
            return hasNext;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _innerEnum.DisposeAsync();
            }
            finally
            {
                if (_auditScope != null)
                {
                    await _auditScope.DisposeAsync();
                }
            }

        }
    }
}

