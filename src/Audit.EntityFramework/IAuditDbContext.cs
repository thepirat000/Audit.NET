#if EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using Audit.Core;
using System.Collections.Generic;
using System;
using Audit.EntityFramework.ConfigurationApi;

namespace Audit.EntityFramework
{
    public interface IAuditDbContext
    {
        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the context name). 
        /// Can contain the following placeholders: 
        ///  - {context}: replaced with the Db Context type name.
        ///  - {database}: replaced with the database name.
        /// </summary>
        string AuditEventType { get; set; }
        /// <summary>
        /// Indicates if the Audit is disabled.
        /// Default is false.
        /// </summary>
        bool AuditDisabled { get; set; }
        /// <summary>
        /// To indicate if the output should contain the modified entities objects. (Default is false)
        /// </summary>
        bool IncludeEntityObjects { get; set; }
        /// <summary>
        /// To indicate if the entity validations should be avoided and excluded from the audit output. (Default is false)
        /// </summary>
        bool ExcludeValidationResults { get; set; }
        /// <summary>
        /// To indicate the audit operation mode. (Default is OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        AuditOptionMode Mode { get; set; }
        /// <summary>
        /// To indicate the Audit Data Provider to use. (Default is NULL to use the configured default data provider). 
        /// </summary>
        AuditDataProvider AuditDataProvider { get; set; }
        /// <summary>
        /// To indicate a custom audit scope factory. (Default is NULL to use the Audit.Core.Configuration.DefaultAuditScopeFactory). 
        /// </summary>
        IAuditScopeFactory AuditScopeFactory { get; set; }
        /// <summary>
        /// Optional custom fields added to the audit event
        /// </summary>
        Dictionary<string, object> ExtraFields { get; }
        /// <summary>
        /// To indicate if the Transaction Id retrieval should be ignored. If set to <c>true</c> the Transations Id will not be included on the output.
        /// </summary>
        bool ExcludeTransactionId { get; set; }
        /// <summary>
        /// To indicate if the audit event should be saved before the entity saving operation takes place. 
        /// Default is false to save the audit event after the entity saving operation completes or fails.
        /// </summary>
        bool EarlySavingAudit { get; set; }
        DbContext DbContext { get; }
        /// <summary>
        /// A collection of settings per entity type.
        /// </summary>
        Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        /// <summary>
        /// Called after the audit scope is created.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        void OnScopeCreated(IAuditScope auditScope);
        /// <summary>
        /// Called after the EF operation execution and before the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        void OnScopeSaving(IAuditScope auditScope);
        /// <summary>
        /// Called after the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        void OnScopeSaved(IAuditScope auditScope);
#if EF_FULL
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        bool IncludeIndependantAssociations { get; set; }
#endif

        /// <summary>
        /// Value to indicate if the original values of the audited entities should be queried from database explicitly, before any modification or delete operation.
        /// Default is false.
        /// </summary>
        bool ReloadDatabaseValues { get; set; }
    }
}
