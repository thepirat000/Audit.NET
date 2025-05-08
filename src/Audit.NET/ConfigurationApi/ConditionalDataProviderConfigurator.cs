using System;
using System.Collections.Generic;
using Audit.Core.Providers.Wrappers;

namespace Audit.Core.ConfigurationApi
{
    public class ConditionalDataProviderConfigurator : IConditionalDataProviderConfigurator
    {
        internal List<ConditionalDataProvider.GuardCondition> _guardConditions = new List<ConditionalDataProvider.GuardCondition>();

        /// <inheritdoc />
        public IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, Func<AuditEvent, IAuditDataProvider> dataProviderFactory)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = guardCondition,
                DataProvider = new DeferredDataProvider(dataProviderFactory)
            });

            return this;
        }

        /// <inheritdoc />
        public IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, Func<IAuditDataProvider> dataProviderInitializer)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = guardCondition,
                DataProvider = new LazyDataProvider(dataProviderInitializer)
            });

            return this;
        }

        /// <inheritdoc />
        public IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, IAuditDataProvider dataProvider)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = guardCondition,
                DataProvider = dataProvider
            });

            return this;
        }

        /// <inheritdoc />
        public void Otherwise(Func<AuditEvent, IAuditDataProvider> dataProviderFactory)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = _ => true,
                DataProvider = new DeferredDataProvider(dataProviderFactory)
            });
        }

        /// <inheritdoc />
        public void Otherwise(Func<IAuditDataProvider> dataProviderInitializer)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = _ => true,
                DataProvider = new LazyDataProvider(dataProviderInitializer)
            });
        }

        /// <inheritdoc />
        public void Otherwise(IAuditDataProvider dataProvider)
        {
            _guardConditions.Add(new ConditionalDataProvider.GuardCondition()
            {
                Guard = _ => true,
                DataProvider = dataProvider
            });
        }
    }
}