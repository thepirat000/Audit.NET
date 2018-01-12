using System;
using Audit.Core;

namespace Audit.WCF
{
    internal class AuditScopeState
    {
        public AuditScope AuditScope { get; set; }
        public AsyncCallback OriginalUserCallback { get; set; }
        public object OriginalUserState { get; set; }
    }
}