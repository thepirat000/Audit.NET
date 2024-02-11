using System;
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
        public ICreationPolicyConfigurator UseDeferredFactory(Func<AuditEvent, AuditDataProvider> dataProviderFactory)
        {
            Configuration.DataProvider = new DeferredDataProvider(dataProviderFactory);
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseLazyFactory(Func<AuditDataProvider> dataProviderInitializer)
        {
            Configuration.DataProvider = new LazyDataProvider(dataProviderInitializer);
            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator UseConditional(Action<IConditionalDataProviderConfigurator> config)
        {
            Configuration.DataProvider = new ConditionalDataProvider(config);
            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator Use(AuditDataProvider provider)
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
            var fileLogConfig = new FileLogProviderConfigurator();
            config.Invoke(fileLogConfig);
            return UseFileLogProvider(fileLogConfig._directoryPath, fileLogConfig._filenamePrefix, fileLogConfig._directoryPathBuilder, fileLogConfig._filenameBuilder);
        }
        public ICreationPolicyConfigurator UseCustomProvider(AuditDataProvider provider)
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
            var eventLogConfig = new EventLogProviderConfigurator();
            config.Invoke(eventLogConfig);
            return UseEventLogProvider(eventLogConfig._logName, eventLogConfig._sourcePath, eventLogConfig._machineName, eventLogConfig._messageBuilder);
        }
#endif
        public ICreationPolicyConfigurator UseFileLogProvider(string directoryPath = "", string filenamePrefix = "", 
            Func<AuditEvent, string> directoryPathBuilder = null, Func<AuditEvent, string> filenameBuilder = null)
        {
            var fdp = new FileDataProvider()
            {
                DirectoryPath = directoryPath,
                FilenamePrefix = filenamePrefix,
                DirectoryPathBuilder = directoryPathBuilder,
                FilenameBuilder = filenameBuilder
            };
            Configuration.DataProvider = fdp;

            return new CreationPolicyConfigurator();
        }

        public ICreationPolicyConfigurator UseInMemoryProvider()
        {
            Configuration.DataProvider = new InMemoryDataProvider();

            return new CreationPolicyConfigurator();
        }
    }
}