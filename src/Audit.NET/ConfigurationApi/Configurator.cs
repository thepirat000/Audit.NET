using System;
using System.Collections.Concurrent;

using Audit.Core.Providers;
using Audit.Core.Providers.Wrappers;

namespace Audit.Core.ConfigurationApi
{
    public class Configurator : IConfigurator
    {
        public IConfigurator AuditDisabled(bool auditDisabled)
        {
            Configuration.AuditDisabled = auditDisabled;
            return this;
        }

        public IConfigurator JsonAdapter(IJsonAdapter adapter)
        {
            Configuration.JsonAdapter = adapter;
            return this;
        }

        public IConfigurator JsonAdapter<T>() where T : IJsonAdapter
        {
            Configuration.JsonAdapter = Activator.CreateInstance<T>();
            return this;
        }

        public IConfigurator IncludeStackTrace(bool includeStackTrace = true)
        {
            Configuration.IncludeStackTrace = includeStackTrace;
            return this;
        }

        public IConfigurator IncludeActivityTrace(bool includeActivityTrace = true)
        {
            Configuration.IncludeActivityTrace = includeActivityTrace;
            return this;
        }

        public IConfigurator StartActivityTrace(bool startActivityTrace = true)
        {
            Configuration.StartActivityTrace = startActivityTrace;
            return this;
        }

        public ICreationPolicyConfigurator UseNullProvider()
        {
            var dataProvider = new NullDataProvider();
            Configuration.DataProvider = dataProvider;
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator Use(Action<IDynamicDataProviderConfigurator> config)
        {
            return UseDynamicProvider(config);
        }
        public ICreationPolicyConfigurator UseDeferredFactory(Func<AuditEvent, IAuditDataProvider> dataProviderFactory)
        {
            Configuration.DataProvider = new DeferredDataProvider(dataProviderFactory);
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseLazyFactory(Func<IAuditDataProvider> dataProviderInitializer)
        {
            Configuration.DataProvider = new LazyDataProvider(dataProviderInitializer);
            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator UseConditional(Action<IConditionalDataProviderConfigurator> config)
        {
            Configuration.DataProvider = new ConditionalDataProvider(config);
            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator Use(IAuditDataProvider provider)
        {
            return UseCustomProvider(provider);
        }
        public ICreationPolicyConfigurator UseDynamicProvider(Action<IDynamicDataProviderConfigurator> config)
        {
            var dataProvider = new DynamicDataProvider();
            var dynamicConfig = new DynamicDataProviderConfigurator(dataProvider);
            config.Invoke(dynamicConfig);
            Configuration.DataProvider = dataProvider;
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseDynamicAsyncProvider(Action<IDynamicAsyncDataProviderConfigurator> config)
        {
            var dataProvider = new DynamicAsyncDataProvider();
            var dynamicConfig = new DynamicAsyncDataProviderConfigurator(dataProvider);
            config.Invoke(dynamicConfig);
            Configuration.DataProvider = dataProvider;
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseFileLogProvider(Action<IFileLogProviderConfigurator> config)
        {
            Configuration.DataProvider = new FileDataProvider(config);
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseCustomProvider(IAuditDataProvider provider)
        {
            Configuration.DataProvider = provider;
            return new CreationPolicyConfigurator();
        }

#if NET462 || NET472
        public ICreationPolicyConfigurator UseEventLogProvider(string logName = "Application", string sourcePath = "Application", string machineName = ".", Func<AuditEvent, string> messageBuilder = null)
        {
            Configuration.DataProvider = new EventLogDataProvider()
            {
                LogName = logName,
                SourcePath = sourcePath,
                MachineName = machineName,
                MessageBuilder = messageBuilder
            };
            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator UseEventLogProvider(Action<IEventLogProviderConfigurator> config)
        {
            Configuration.DataProvider = new EventLogDataProvider(config);
            return new CreationPolicyConfigurator();
        }
#endif

        public ICreationPolicyConfigurator UseInMemoryProvider()
        {
            Configuration.DataProvider = new InMemoryDataProvider();

            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator UseInMemoryProvider(out InMemoryDataProvider dataProvider)
        {
            dataProvider = new InMemoryDataProvider();

            Configuration.DataProvider = dataProvider;
            
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Uses a BlockingCollection to store the audit events in memory.
        /// </summary>
        /// <param name="config">The BlockingCollection provider configuration.</param>
        public ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider(Action<IBlockingCollectionProviderConfigurator> config)
        {
            Configuration.DataProvider = new BlockingCollectionDataProvider(config);

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Uses a BlockingCollection to store the audit events in memory.
        /// </summary>
        public ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider()
        {
            Configuration.DataProvider = new BlockingCollectionDataProvider();

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Uses a BlockingCollection to store the audit events in memory.
        /// </summary>
        /// <param name="config">The BlockingCollection provider configuration.</param>
        /// <param name="blockingCollection">The created BlockingCollection instance</param>
        public ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider(Action<IBlockingCollectionProviderConfigurator> config, out BlockingCollection<AuditEvent> blockingCollection)
        {
            var dataProvider = new BlockingCollectionDataProvider(config);
            Configuration.DataProvider = dataProvider;
            blockingCollection = dataProvider.GetBlockingCollection();
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Uses a BlockingCollection to store the audit events in memory.
        /// </summary>
        /// <param name="blockingCollection">The created BlockingCollection instance</param>
        public ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider(out BlockingCollection<AuditEvent> blockingCollection)
        {
            var dataProvider = new BlockingCollectionDataProvider();
            Configuration.DataProvider = dataProvider;
            blockingCollection = dataProvider.GetBlockingCollection();
            return new CreationPolicyConfigurator();
        }

        /// <inheritdoc />
        public ICreationPolicyConfigurator UseActivityProvider(Action<IActivityProviderConfigurator> config)
        {
            var dataProvider = new ActivityDataProvider(config);
            Configuration.DataProvider = dataProvider;
            return new CreationPolicyConfigurator();
        }
    }
}