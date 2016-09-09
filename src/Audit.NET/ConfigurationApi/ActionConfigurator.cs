using System;

namespace Audit.Core.ConfigurationApi
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
            Configuration.ResetCustomActions();
            return this;
        }
    }
}