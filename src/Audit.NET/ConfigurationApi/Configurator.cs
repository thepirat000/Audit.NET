using System;
using Audit.Core.Providers;

namespace Audit.Core.ConfigurationApi
{
    public class Configurator : IConfigurator
    {
        public ICreationPolicyConfigurator UseDynamicProvider(Action<IDynamicDataProviderConfigurator> config)
        {
            var dataProvider = new DynamicDataProvider();
            var dynamicConfig = new DynamicDataProviderConfigurator(dataProvider);
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
#if NET45
        public ICreationPolicyConfigurator UseEventLogProvider(string logName = "Application", string sourcePath = "Application", string machineName = ".")
        {
            Configuration.DataProvider = new EventLogDataProvider()
            {
                LogName = logName,
                SourcePath = sourcePath,
                MachineName = machineName
            };
            return new CreationPolicyConfigurator();
        }
        public ICreationPolicyConfigurator UseEventLogProvider(Action<IEventLogProviderConfigurator> config)
        {
            var eventLogConfig = new EventLogProviderConfigurator();
            config.Invoke(eventLogConfig);
            return UseEventLogProvider(eventLogConfig._logName, eventLogConfig._sourcePath, eventLogConfig._machineName);
        }
#endif
        public ICreationPolicyConfigurator UseFileLogProvider(string directoryPath = "", string filenamePrefix = "", 
            Func<AuditEvent, string> directoryPathBuilder = null, Func<AuditEvent, string> filenameBuilder = null)
        {
            Configuration.DataProvider = new FileDataProvider()
            {
                DirectoryPath = directoryPath,
                FilenamePrefix = filenamePrefix,
                DirectoryPathBuilder = directoryPathBuilder,
                FilenameBuilder = filenameBuilder
            };
            return new CreationPolicyConfigurator();
        }
    }
}