#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#elif NET45
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
#endif
using Audit.Core;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    public interface IAuditDbContext
    {
        string AuditEventType { get; set; }
        bool AuditDisabled { get; set; }
        bool IncludeEntityObjects { get; set; }
        AuditOptionMode Mode { get; set; }
        AuditDataProvider AuditDataProvider { get; set; }
        Dictionary<string, object> ExtraFields { get; }
        DbContext DbContext { get; }
        void OnScopeSaving(AuditScope auditScope);
        void OnScopeCreated(AuditScope auditScope);
#if NET45
        bool IncludeIndependantAssociations { get; set; }
#endif
    }
}
