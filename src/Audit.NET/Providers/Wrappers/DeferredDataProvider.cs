using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core.Providers.Wrappers
{
    /// <summary>
    /// A deferred data provider that allows to defer the data provider creation until the audit event is about to be saved. The data provider factory method will be called for each audit event being saved.
    /// </summary>
    public class DeferredDataProvider : AuditDataProvider
    {
        private Func<AuditEvent, IAuditDataProvider> _factory;

        public Func<AuditEvent, IAuditDataProvider> Factory
        {
            set => _factory = value;
        }

        public DeferredDataProvider()
        {
        }

        public DeferredDataProvider(Func<AuditEvent, IAuditDataProvider> dataProviderFactory)
        {
            _factory = dataProviderFactory;
        }

        /// <summary>
        /// Returns the data provider for a given audit event.
        /// </summary>
        protected virtual IAuditDataProvider GetDataProvider(AuditEvent auditEvent)
        {
            return _factory.Invoke(auditEvent);
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider(auditEvent);

            return dataProvider?.InsertEvent(auditEvent);
        }

        /// <inheritdoc />
        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider(auditEvent);

            return dataProvider?.InsertEventAsync(auditEvent, cancellationToken) ?? Task.FromResult<object>(null);
        }

        /// <inheritdoc />
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider(auditEvent);

            dataProvider?.ReplaceEvent(eventId, auditEvent);
        }

        /// <inheritdoc />
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider(auditEvent);

            return dataProvider?.ReplaceEventAsync(eventId, auditEvent, cancellationToken) ?? Task.CompletedTask;
        }

        /// <inheritdoc />
        public override object CloneValue<T>(T value, AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider(auditEvent);

            return dataProvider != null ? dataProvider.CloneValue(value, auditEvent) : base.CloneValue(value, auditEvent);
        }

        /// <inheritdoc />
        public override T GetEvent<T>(object eventId)
        {
            var dataProvider = GetDataProvider(null);

            return dataProvider?.GetEvent<T>(eventId);
        }

        /// <inheritdoc />
        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider(null);

            return dataProvider?.GetEventAsync<T>(eventId, cancellationToken);
        }
    }
}