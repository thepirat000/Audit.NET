#if NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET461
using Microsoft.EntityFrameworkCore;
#elif NET45
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
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
        public virtual AuditOptionMode Mode { get; set; }
        public virtual Dictionary<string, object> ExtraFields { get; set; }
        public virtual Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        public virtual AuditDataProvider AuditDataProvider { get; set; }
        public virtual void OnScopeCreated(AuditScope auditScope) { }
        public virtual void OnScopeSaving(AuditScope auditScope) { }
        public bool ExcludeTransactionId { get; set; }
#if NET45
        public virtual bool IncludeIndependantAssociations { get; set; }
#endif
        public DefaultAuditContext()
        {
        }
        public DefaultAuditContext(DbContext dbContext)
        {
            DbContext = dbContext;
        }
    }
}
