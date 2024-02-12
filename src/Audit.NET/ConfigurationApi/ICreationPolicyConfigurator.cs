namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the default Creation Policy
    /// </summary>
    public interface ICreationPolicyConfigurator
    {
        /// <summary>
        /// Specifies the event creation policy to use.
        /// </summary>
        /// <param name="policy">The event creation policy.</param>
        IActionConfigurator WithCreationPolicy(EventCreationPolicy policy);
        /// <summary>
        /// Use an "Insert On End" creation policy. The events are inserted when the scope ends (i.e. when disposing).
        /// </summary>
        IActionConfigurator WithInsertOnEndCreationPolicy();
        /// <summary>
        /// Use an "Insert On Start - Insert On End" creation policy. The events are inserted when the scope starts, and inserted again when the scope ends.
        /// </summary>
        IActionConfigurator WithInsertOnStartInsertOnEndCreationPolicy();
        /// <summary>
        /// Use an "Insert On Start - Replace On End" creation policy. The events are inserted when the scope starts, and replaced when the scope ends.
        /// </summary>
        IActionConfigurator WithInsertOnStartReplaceOnEndCreationPolicy();
        /// <summary>
        /// Use a "Manual" creation policy. The events are manually inserted/replaced by calling .Save() method on the AuditScope.
        /// </summary>
        IActionConfigurator WithManualCreationPolicy();
    }
}