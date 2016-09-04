using System;

namespace Audit.Core.Configuration
{
    public class ActionEventSelector : IActionEventSelector
    {
        public void OnEventSaving(Action<AuditScope> action)
        {
            AuditConfiguration.AddCustomAction(ActionType.OnEventSaving, action);
        }

        public void OnScopeCreated(Action<AuditScope> action)
        {
            AuditConfiguration.AddCustomAction(ActionType.OnScopeCreated, action);
        }
    }
}