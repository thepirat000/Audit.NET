using Audit.Core.ConfigurationApi;
using Audit.Core.Providers;
using System;

namespace Audit.Core
{
    public static class EvenLogProviderConfigurator
    {
        public static ICreationPolicyConfigurator UseEventLogProvider(this IConfigurator configurator, string logName = "Application", string sourcePath = "Application", string machineName = ".", Func<AuditEvent, string> messageBuilder = null)
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
        
        public static ICreationPolicyConfigurator UseEventLogProvider(this IConfigurator configurator, Action<IEventLogProviderConfigurator> config)
        {
            var eventLogConfig = new EventLogProviderConfigurator();
            config.Invoke(eventLogConfig);
            return UseEventLogProvider(configurator, eventLogConfig._logName, eventLogConfig._sourcePath, eventLogConfig._machineName, eventLogConfig._messageBuilder);
        }
    }
}
