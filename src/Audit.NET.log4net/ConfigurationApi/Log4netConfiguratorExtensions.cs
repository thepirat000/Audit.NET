using System;
using Audit.Core.ConfigurationApi;
using Audit.log4net;
using Audit.log4net.Configuration;
using Audit.log4net.Providers;
using log4net;

namespace Audit.Core
{
    public static class Log4netConfiguratorExtensions
    {
        /// <summary>
        /// Store the audit events using Apache log4net
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <param name="config">The log4net provider configuration.</param>
        public static ICreationPolicyConfigurator UseLog4net(this IConfigurator configurator, Action<ILog4netConfigurator> config)
        {
            var log4netConfig = new Log4netConfigurator();
            config.Invoke(log4netConfig);
            Configuration.DataProvider = new Log4netDataProvider()
            {
                LogLevelBuilder = log4netConfig._logLevelBuilder,
                LoggerBuilder = log4netConfig._loggerBuilder,
                LogMessageBuilder = log4netConfig._messageBuilder
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the audit events using Apache log4net with the default configuration
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        public static ICreationPolicyConfigurator UseLog4net(this IConfigurator configurator)
        {
            Configuration.DataProvider = new Log4netDataProvider();
            return new CreationPolicyConfigurator();
        }
    }
}
