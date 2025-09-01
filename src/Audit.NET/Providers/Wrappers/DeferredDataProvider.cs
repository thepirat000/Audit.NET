using System;

namespace Audit.Core.Providers.Wrappers
{
    /// <summary>
    /// A deferred data provider that allows to defer the data provider creation until the audit event is about to be saved. The data provider factory method will be called for each audit event being saved.
    /// </summary>
    public class DeferredDataProvider : WrapperDataProvider
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
        protected override IAuditDataProvider GetDataProvider(AuditEvent auditEvent)
        {
            return _factory.Invoke(auditEvent);
        }
    }
}