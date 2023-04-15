using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core.Providers
{
    /// <summary>
    /// A null data provider. Useful to disable the audit logs. This data provider just ignores the events.
    /// </summary>
    public class NullDataProvider : AuditDataProvider
    {
        public override object InsertEvent(AuditEvent auditEvent)
        {
            return null;
        }
        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>(null);
        }
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            return;
        }
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            return Task.Delay(0);
        }
        public override T GetEvent<T>(object eventId)
        {
            return default(T);
        }
        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }
    }
}
