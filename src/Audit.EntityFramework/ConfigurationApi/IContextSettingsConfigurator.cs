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
        /// Sets the indicator to avoid and exclude entity validations from the audit output.
        /// </summary>
        /// <param name="exclude">if set to <c>true</c> the entity validations will not be executed and excluded from the audit output.</param>
        IContextSettingsConfigurator<T> ExcludeValidationResults(bool exclude = true);
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
        /// <summary>
        /// Value to indicate if the audit event should be saved before the entity saving operation takes place. 
        /// Default is false to save the audit event after the entity saving operation completes or fails.
        /// </summary>
        /// <param name="earlySaving">if set to <c>true</c> the audit event will be saved before the entity saving operation takes place.</param>
        IContextSettingsConfigurator<T> EarlySavingAudit(bool earlySaving = true);

#if EF_FULL
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        /// <param name="include">if set to <c>true</c> the serialized entities will be included.</param>
        IContextSettingsConfigurator<T> IncludeIndependantAssociations(bool include = true);
#endif
        /// <summary>
        /// Value to indicate if the original values of the audited entities should be queried from database explicitly, before saving the audit event.
        /// </summary>
        /// <param name="reloadDatabaseValues">if set to <c>true</c> the original values will be queried from database.</param>
        IContextSettingsConfigurator<T> ReloadDatabaseValues(bool reloadDatabaseValues = true);
    }
}
 