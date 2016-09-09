using System;

namespace Audit.Core.ConfigurationApi
{
    public interface IActionEventSelector
    {
        void OnScopeCreated(Action<AuditScope> action);
        void OnEventSaving(Action<AuditScope> action);
    }
}