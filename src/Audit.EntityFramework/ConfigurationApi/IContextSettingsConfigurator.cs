using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// The settings configuration for an AuditDbContext
    /// </summary>
    /// <typeparam name="T">The AuditDbContext specific type</typeparam>
    public interface IContextSettingsConfigurator<T>
        where T : IAuditDbContext
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
        /// <summary>
        /// Sets the configuration for a specific entity (table)
        /// </summary>
        /// <param name="config">The configuration.</param>
        IContextSettingsConfigurator<T> ForEntity<TEntity>(Action<IContextEntitySetting<TEntity>> config);
        /// <summary>
        /// Value to indicate if the Transaction Id retrieval should be ignored.
        /// </summary>
        /// <param name="exclude">if set to <c>true</c> the Transation Id will not be included on the output.</param>
        IContextSettingsConfigurator<T> ExcludeTransactionId(bool exclude = true);
#if NET45
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        /// <param name="include">if set to <c>true</c> the serialized entities will be included.</param>
        IContextSettingsConfigurator<T> IncludeIndependantAssociations(bool include = true);
#endif
    }
}
 