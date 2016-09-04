using System;

namespace Audit.Core.Configuration
{
    public interface IActionEventSelector
    {
        void OnScopeCreated(Action<AuditScope> action);
        void OnEventSaving(Action<AuditScope> action);
    }
}