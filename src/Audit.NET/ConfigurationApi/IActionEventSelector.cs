using System;
using System.Threading.Tasks;

namespace Audit.Core.ConfigurationApi
{
    public interface IActionEventSelector
    {
        void OnScopeCreated(Action<AuditScope> action);
        void OnEventSaving(Action<AuditScope> action);
        void OnEventSaved(Action<AuditScope> action);
        void OnScopeCreated(Func<AuditScope, Task> action);
        void OnEventSaving(Func<AuditScope, Task> action);
        void OnEventSaved(Func<AuditScope, Task> action);
        void OnScopeDisposed(Func<AuditScope, Task> action);
        void OnScopeDisposed(Action<AuditScope> action);
    }
}