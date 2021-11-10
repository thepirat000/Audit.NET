using Audit.AzureCosmos.ConfigurationApi;
using Audit.AzureCosmos.Providers;
using Audit.Core.ConfigurationApi;
using System;

namespace Audit.Core
{
    public static class AzureCosmosConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Cosmos database.
        /// </summary>
        /// <param name="config">The Azure Cosmos provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureCosmos(
            this IConfigurator configurator, Action<IAzureCosmosProviderConfigurator> config)
        {
            Configuration.DataProvider = new AzureCosmosDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
