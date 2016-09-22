using System;
using Audit.Core.ConfigurationApi;
using Audit.AzureTableStorage.Providers;
using Audit.AzureTableStorage.ConfigurationApi;

namespace Audit.Core
{
    public static class AzureStorageConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        /// <param name="containerName">The Azure Storage container name.</param>
        /// <param name="blobNameBuilder">A builder that returns a unique name for the blob (can contain folders).</param>
        public static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, string connectionString = null,
            string containerName = "event", Func<AuditEvent, string> blobNameBuilder = null)
        {
            Configuration.DataProvider = new AzureBlobDataProvider()
            {
                ConnectionString = connectionString,
                ContainerName = containerName,
                BlobNameBuilder = blobNameBuilder
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="config">The Azure Blob provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, Action<IAzureBlobProviderConfigurator> config)
        {
            var blobConfig = new AzureBlobProviderConfigurator();
            config.Invoke(blobConfig);
            return UseAzureBlobStorage(configurator, blobConfig._connectionString, blobConfig._containerName, blobConfig._blobNameBuilder);
        }
    }
}
