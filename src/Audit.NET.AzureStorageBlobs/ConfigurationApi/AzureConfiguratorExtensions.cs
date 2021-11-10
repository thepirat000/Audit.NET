using Audit.AzureStorageBlobs.ConfigurationApi;
using Audit.AzureStorageBlobs.Providers;
using Audit.Core.ConfigurationApi;
using System;

namespace Audit.Core
{
    public static class AzureConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="config">The Azure Storage Blob provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureStorageBlobs(this IConfigurator configurator, Action<IAzureBlobConnectionConfigurator> config)
        {
            Configuration.DataProvider = new AzureStorageBlobDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
