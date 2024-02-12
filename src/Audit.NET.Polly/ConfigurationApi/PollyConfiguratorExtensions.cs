using System;
using Audit.Core.ConfigurationApi;
using Audit.Polly.Configuration;
using Audit.Polly.Providers;

namespace Audit.Core
{
    public static class PollyConfiguratorExtensions
    {
        /// <summary>
        /// Uses Polly to handle resilience policies for the data provider.
        /// </summary>
        public static ICreationPolicyConfigurator UsePolly(this IConfigurator configurator, Action<IPollyProviderConfigurator> config)
        {
            Configuration.DataProvider = new PollyDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}