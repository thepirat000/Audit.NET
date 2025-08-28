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
    /// <summary>
    /// Empty default implementation of IAuditDbContext
    /// </summary>
    public class DefaultAuditContext : IAuditDbContext
    {
        public virtual DbContext DbContext { get; set; }
        public virtual string AuditEventType { get; set; }
        public virtual bool AuditDisabled { get; set; }
        public virtual bool IncludeEntityObjects { get; set; }
        public virtual bool ExcludeValidationResults { get; set; }
        public virtual AuditOptionMode Mode { get; set; }
        public virtual Dictionary<string, object> ExtraFields { get; set; }
        public virtual Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        public virtual IAuditDataProvider AuditDataProvider { get; set; }
        public virtual IAuditScopeFactory AuditScopeFactory { get; set; }
        public virtual void OnScopeCreated(IAuditScope auditScope) { }
        public virtual void OnScopeSaving(IAuditScope auditScope) { }
        public virtual void OnScopeSaved(IAuditScope auditScope)  { }
        public bool ExcludeTransactionId { get; set; }
#if EF_FULL
        public virtual bool IncludeIndependantAssociations { get; set; }
#endif
        public bool ReloadDatabaseValues { get; set; }
        public bool MapChangesByColumn { get; set; }

        public Dictionary<Type, HashSet<string>> IncludedPropertyNames { get; set; }

        public DefaultAuditContext()
        {
        }
        public DefaultAuditContext(DbContext dbContext)
        {
            DbContext = dbContext;
        }
    }
}
