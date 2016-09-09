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
    }
}