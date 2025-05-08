using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core.Providers.Wrappers
{
    /// <summary>
    /// A lazy data provider that allows to lazily instantiate the data provider to use. The data provider factory method will be called only once; the first time it's needed.
    /// </summary>
    public class LazyDataProvider : AuditDataProvider
    {
        private Lazy<IAuditDataProvider> _factory;

        public Func<IAuditDataProvider> Factory
        {
            set => _factory = new Lazy<IAuditDataProvider>(value);
        }

        public LazyDataProvider()
        {
        }

        public LazyDataProvider(Func<IAuditDataProvider> dataProviderFactory)
        {
            _factory = new Lazy<IAuditDataProvider>(dataProviderFactory);
        }

        protected virtual IAuditDataProvider GetDataProvider()
        {
            return _factory.Value;
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider();
            
            return dataProvider?.InsertEvent(auditEvent);
        }

        /// <inheritdoc />
        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider();
            
            return dataProvider?.InsertEventAsync(auditEvent, cancellationToken);
        }

        /// <inheritdoc />
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider();

            dataProvider?.ReplaceEvent(eventId, auditEvent);
        }

        /// <inheritdoc />
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider();
            
            return dataProvider?.ReplaceEventAsync(eventId, auditEvent, cancellationToken);
        }

        /// <inheritdoc />
        public override object CloneValue<T>(T value, AuditEvent auditEvent)
        {
            var dataProvider = GetDataProvider();

            return dataProvider != null ? dataProvider.CloneValue(value, auditEvent) : base.CloneValue(value, auditEvent);
        }

        /// <inheritdoc />
        public override T GetEvent<T>(object eventId)
        {
            var dataProvider = GetDataProvider();
            
            return dataProvider?.GetEvent<T>(eventId);
        }

        /// <inheritdoc />
        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var dataProvider = GetDataProvider();
            
            return dataProvider?.GetEventAsync<T>(eventId, cancellationToken);
        }
    }
}