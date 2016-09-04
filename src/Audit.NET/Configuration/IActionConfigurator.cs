using System;

namespace Audit.Core.Configuration
{
    public interface IActionConfigurator
    {
        /// <summary>
        /// Attaches a new global action to the scopes.
        /// </summary>
        /// <param name="actionSelector">The action configuration.</param>
        IActionConfigurator WithAction(Action<IActionEventSelector> actionSelector);
        /// <summary>
        /// Removes all the global actions.
        /// </summary>
        IActionConfigurator ResetActions();
    }
}