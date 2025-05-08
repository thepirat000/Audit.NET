using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core.ConfigurationApi;

namespace Audit.Core.Providers.Wrappers
{
    /// <summary>
    /// This provider enables the configuration of different data providers based on conditions related to the audit event.
    /// </summary>
    public class ConditionalDataProvider : AuditDataProvider
    {
        public class GuardCondition
        {
            public Func<AuditEvent, bool> Guard { get; set; }
            public IAuditDataProvider DataProvider { get; set; }
        }

        /// <summary>
        /// The list of guarded data providers.
        /// </summary>
        public List<GuardCondition> GuardConditions { get; set; } = new List<GuardCondition>();

        public ConditionalDataProvider()
        {
        }

        public ConditionalDataProvider(Action<IConditionalDataProviderConfigurator> config)
        {
            var conditionalConfig = new ConditionalDataProviderConfigurator();
            if (config != null)
            {
                config.Invoke(conditionalConfig);
                GuardConditions = conditionalConfig._guardConditions;
            }
        }

        /// <summary>
        /// Returns the data provider for a given audit event, or NULL if no condition is met.
        /// </summary>
        protected virtual IAuditDataProvider GetDataProvider(AuditEvent auditEvent)
        {
            if (GuardConditions != null)
            {
                foreach (var config in GuardConditions)
                {
                    if (config.Guard(auditEvent))
                    {
                        return config.DataProvider;
                    }
                }
            }

            return null;
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