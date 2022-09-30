#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
#endif
using Audit.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Audit.EntityFramework.ConfigurationApi;

namespace Audit.EntityFramework
{
    public interface IAuditDbContext
    {
        string AuditEventType { get; set; }
        bool AuditDisabled { get; set; }
        bool IncludeEntityObjects { get; set; }
        bool ExcludeValidationResults { get; set; }
        AuditOptionMode Mode { get; set; }
        AuditDataProvider AuditDataProvider { get; set; }
        IAuditScopeFactory AuditScopeFactory { get; set; }
        Dictionary<string, object> ExtraFields { get; }
        DbContext DbContext { get; }
        Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        void OnScopeSaving(IAuditScope auditScope);
        void OnScopeSaved(IAuditScope auditScope);
        void OnScopeCreated(IAuditScope auditScope);
        bool ExcludeTransactionId { get; set; }
        bool EarlySavingAudit { get; set; }
#if EF_FULL
        bool IncludeIndependantAssociations { get; set; }
#endif
    }
}
