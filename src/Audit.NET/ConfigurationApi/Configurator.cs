using System;
using Audit.Core.Providers;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

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
        public ICreationPolicyConfigurator UseFactory(Func<AuditDataProvider> dataProviderFactory)
        {
            Configuration.DataProviderFactory = dataProviderFactory;
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator Use(Func<AuditDataProvider> dataProviderFactory)
        {
            return UseFactory(dataProviderFactory);
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
#if NET45 || NET461
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