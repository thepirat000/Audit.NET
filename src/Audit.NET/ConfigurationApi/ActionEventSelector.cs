using System;
using System.Threading.Tasks;

namespace Audit.Core.ConfigurationApi
{
    public class ActionEventSelector : IActionEventSelector
    {
        public void OnEventSaved(Action<AuditScope> action)
        {
            Configuration.AddCustomAction(ActionType.OnEventSaved, action);
        }

        public void OnEventSaved(Func<AuditScope, Task> action)
        {
            Configuration.AddCustomAction(ActionType.OnEventSaved, action);
        }

        public void OnEventSaving(Action<AuditScope> action)
        {
            Configuration.AddCustomAction(ActionType.OnEventSaving, action);
        }

        public void OnEventSaving(Func<AuditScope, Task> action)
        {
            Configuration.AddCustomAction(ActionType.OnEventSaving, action);
        }

        public void OnScopeCreated(Action<AuditScope> action)
        {
            Configuration.AddCustomAction(ActionType.OnScopeCreated, action);
        }

        public void OnScopeCreated(Func<AuditScope, Task> action)
        {
            Configuration.AddCustomAction(ActionType.OnScopeCreated, action);
        }

        public void OnScopeDisposed(Func<AuditScope, Task> action)
        {
            Configuration.AddCustomAction(ActionType.OnScopeDisposed, action);
        }

        public void OnScopeDisposed(Action<AuditScope> action)
        {
            Configuration.AddCustomAction(ActionType.OnScopeDisposed, action);
        }
    }
}