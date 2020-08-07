using System;
using System.Threading;
using Audit.Core;

namespace Audit.WCF
{
    //https://blogs.msdn.microsoft.com/carlosfigueira/2011/05/16/wcf-extensibility-ioperationinvoker/
    internal class AuditScopeAsyncResult : IAsyncResult
    {
        private readonly IAsyncResult _originalResult;
        private readonly AuditScopeState _auditScopeState;

        internal AuditScopeAsyncResult(IAsyncResult originalResult, AuditScopeState auditScopeState)
        {
            _originalResult = originalResult;
            _auditScopeState = auditScopeState;
        }

        internal AuditScopeState AuditScopeState => _auditScopeState;
        internal IAuditScope AuditScope => _auditScopeState.AuditScope;
        public bool IsCompleted => _originalResult.IsCompleted;
        public WaitHandle AsyncWaitHandle => _originalResult.AsyncWaitHandle;
        public object AsyncState => _auditScopeState.OriginalUserState;
        public bool CompletedSynchronously => _originalResult.CompletedSynchronously;
        internal IAsyncResult OriginalAsyncResult => _originalResult;
    }
}