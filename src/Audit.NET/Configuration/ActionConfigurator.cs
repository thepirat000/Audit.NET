using System;

namespace Audit.Core.Configuration
{
    public class ActionConfigurator : IActionConfigurator
    {
        public IActionConfigurator WithAction(Action<IActionEventSelector> actionSelector)
        {
            var action = new ActionEventSelector();
            actionSelector.Invoke(action);
            return this;
        }
        public IActionConfigurator ResetActions()
        {
            AuditConfiguration.ResetCustomActions();
            return this;
        }
    }
}