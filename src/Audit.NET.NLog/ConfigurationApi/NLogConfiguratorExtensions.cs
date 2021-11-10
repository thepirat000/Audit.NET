﻿using System;
using Audit.Core.ConfigurationApi;
using Audit.NLog.Configuration;
using Audit.NLog.Providers;

namespace Audit.Core
{
    public static class NLogConfiguratorExtensions
    {
        /// <summary>
        /// Store the audit events using Apache NLog
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <param name="config">The NLog provider configuration.</param>
        public static ICreationPolicyConfigurator UseNLog(this IConfigurator configurator, Action<INLogConfigurator> config)
        {
            var NLogConfig = new NLogConfigurator();
            config.Invoke(NLogConfig);
            Configuration.DataProvider = new NLogDataProvider()
            {
                LogLevelBuilder = NLogConfig._logLevelBuilder,
                LoggerBuilder = NLogConfig._loggerBuilder,
                LogMessageBuilder = NLogConfig._messageBuilder
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the audit events using Apache NLog with the default configuration
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        public static ICreationPolicyConfigurator UseNLog(this IConfigurator configurator)
        {
            Configuration.DataProvider = new NLogDataProvider();
            return new CreationPolicyConfigurator();
        }
    }
}
