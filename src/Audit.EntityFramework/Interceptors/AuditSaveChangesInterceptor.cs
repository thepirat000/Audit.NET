#if EF_CORE_5_OR_GREATER
using Audit.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;

namespace Audit.EntityFramework
{
    /// <summary>
    /// SaveChanges Interceptor for auditing. 
    /// Add an instance of this class to the interceptors collection in your DbContext.
    /// Alternative for inheritance / overriding SaveChanges.
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly DbContextHelper _helper = new DbContextHelper();
        private IAuditDbContext _auditContext;
        private IAuditScope _auditScope;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _auditContext = new DefaultAuditContext(eventData.Context);
            _helper.SetConfig(_auditContext);
            _auditScope = _helper.BeginSaveChanges(_auditContext);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _auditContext = new DefaultAuditContext(eventData.Context);
            _helper.SetConfig(_auditContext);
            _auditScope = await _helper.BeginSaveChangesAsync(_auditContext);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            _helper.EndSaveChanges(_auditContext, _auditScope, result);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await _helper.EndSaveChangesAsync(_auditContext, _auditScope, result);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _helper.EndSaveChanges(_auditContext, _auditScope, 0, eventData.Exception);
            base.SaveChangesFailed(eventData);
        }

        public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            await _helper.EndSaveChangesAsync(_auditContext, _auditScope, 0, eventData.Exception);
            await base.SaveChangesFailedAsync(eventData, cancellationToken);
        }
    }
}
#endif
