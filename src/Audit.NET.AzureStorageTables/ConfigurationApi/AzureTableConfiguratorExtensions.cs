using System;
using Audit.Core.ConfigurationApi;
using Audit.AzureStorageTables.ConfigurationApi;
using Audit.AzureStorageTables.Providers;

namespace Audit.Core
{
    public static class AzureTableConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Table Storage.
        /// </summary>
        /// <param name="configurator">The configurator.</param>
        /// <param name="config">The Azure Table provider configuration as fluent API.</param>
        public static ICreationPolicyConfigurator UseAzureTableStorage(this IConfigurator configurator, Action<IAzureTableConnectionConfigurator> config)
        {
            Configuration.DataProvider = new AzureTableDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
