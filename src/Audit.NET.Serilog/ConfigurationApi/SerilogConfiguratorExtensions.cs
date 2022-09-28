namespace Audit.Core
{
    using System;
    using Audit.Core.ConfigurationApi;
    using Audit.Serilog.Configuration;
    using Audit.NET.Serilog.Providers;

    /// <summary>
    ///     Extensions for serilog.
    /// </summary>
    public static class SerilogConfiguratorExtensions
    {
        /// <summary>
        ///     Store the audit events using Serilog.
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <param name="config">The Serilog provider configuration.</param>
        /// <returns>Policy with serilog.</returns>
        public static ICreationPolicyConfigurator UseSerilog(this IConfigurator configurator, Action<ISerilogConfigurator> config)
        {
            var seriLogConfig = new SerilogConfigurator();
            config.Invoke(seriLogConfig);
            Configuration.DataProvider = new SerilogDataProvider
            {
                LogLevelBuilder = seriLogConfig._logLevelBuilder,
                LoggerBuilder = seriLogConfig._loggerBuilder,
                LogMessageBuilder = seriLogConfig._messageBuilder,
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        ///     Store the audit events using Serilog with the default configuration.
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <returns>Policy with serilog.</returns>
        public static ICreationPolicyConfigurator UseSerilog(this IConfigurator configurator)
        {
            Configuration.DataProvider = new SerilogDataProvider();
            return new CreationPolicyConfigurator();
        }
    }
}
