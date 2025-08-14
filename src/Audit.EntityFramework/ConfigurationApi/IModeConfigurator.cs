namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Configures the context operation mode (OptIn / OptOut)
    /// </summary>
    /// <typeparam name="T">The AuditDbContext type</typeparam>
    public interface IModeConfigurator<T>
    {
        /// <summary>
        /// Uses the opt-out mode.
        /// All the entities are tracked by default, except those explicitly ignored.
        /// </summary>
        IExcludeConfigurator<T> UseOptOut();
        /// <summary>
        /// Uses the opt-in mode.
        /// No entity is tracked by default, except those explicitly included.
        /// </summary>
        IIncludeEntityConfigurator<T> UseOptIn();
        /// <summary>
        /// Resets the previous configuration for this type.
        /// </summary>
        IModeConfigurator<T> Reset();
    }
}