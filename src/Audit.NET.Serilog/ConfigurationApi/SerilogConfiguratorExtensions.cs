using System;

using Audit.Core.ConfigurationApi;
using Audit.Serilog.Configuration;
using Audit.Serilog.Providers;

namespace Audit.Core
{
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
            Configuration.DataProvider = new SerilogDataProvider(config);
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
