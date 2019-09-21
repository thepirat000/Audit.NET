#if NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET461
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#elif NET45
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
        Dictionary<string, object> ExtraFields { get; }
        DbContext DbContext { get; }
        Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        void OnScopeSaving(AuditScope auditScope);
        void OnScopeCreated(AuditScope auditScope);
        bool ExcludeTransactionId { get; set; }
#if NET45
        bool IncludeIndependantAssociations { get; set; }
#endif
    }
}
