namespace Audit.EntityFramework
{
    /// <summary>
    /// The settings configuration for an AuditDbContext
    /// </summary>
    /// <typeparam name="T">The AuditDbContext specific type</typeparam>
    public interface IContextSettingsConfigurator<T>
        where T : AuditDbContext
    {
        /// <summary>
        /// Sets the audit event type to use.
        /// Can contain the following placeholders:
        /// - {context}: replaced with the Db Context type name.
        /// - {database}: replaced with the database name.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <returns>IContextSettingsConfigurator&lt;T&gt;.</returns>
        IContextSettingsConfigurator<T> AuditEventType(string eventType);
        /// <summary>
        /// Sets the indicator to include/exlude the serialized entities on the event output
        /// </summary>
        /// <param name="include">if set to <c>true</c> the serialized entities will be included.</param>
        IContextSettingsConfigurator<T> IncludeEntityObjects(bool include = true);
    }
}