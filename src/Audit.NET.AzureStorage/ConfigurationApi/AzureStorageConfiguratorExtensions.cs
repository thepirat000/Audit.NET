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
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        /// <param name="containerName">The container name for the events.</param>
        /// <param name="blobNameBuilder">A builder that returns a unique name for the blob (can contain folders).</param>
        public static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, string connectionString = null,
            string containerName = "event", Func<AuditEvent, string> blobNameBuilder = null)
        {
            return UseAzureBlobStorage(configurator, connectionString, ev => containerName, blobNameBuilder);
        }
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        /// <param name="containerNameBuilder">A builder that returns a container name for an event.</param>
        /// <param name="blobNameBuilder">A builder that returns a unique name for the blob (can contain folders).</param>
        public static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, string connectionString = null,
            Func<AuditEvent, string> containerNameBuilder = null, Func<AuditEvent, string> blobNameBuilder = null)
        {
            return UseAzureBlobStorage(configurator, ev => connectionString, containerNameBuilder, blobNameBuilder);
        }
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="connectionStringBuilder">A builder that returns a connection string for an event.</param>
        /// <param name="containerNameBuilder">A builder that returns a container name for an event.</param>
        /// <param name="blobNameBuilder">A builder that returns a unique name for the blob (can contain folders).</param>
        private static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, Func<AuditEvent, string> connectionStringBuilder = null,
            Func<AuditEvent, string> containerNameBuilder = null, Func<AuditEvent, string> blobNameBuilder = null)
        {
            Configuration.DataProvider = new AzureBlobDataProvider()
            {
                ConnectionStringBuilder = connectionStringBuilder,
                ContainerNameBuilder = containerNameBuilder,
                BlobNameBuilder = blobNameBuilder
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="config">The Azure Blob provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, Action<IAzureBlobProviderConfigurator> config)
        {
            var blobConfig = new AzureBlobProviderConfigurator();
            config.Invoke(blobConfig);

            if (blobConfig._eventConfig._useActiveDirectory)
            {
                Configuration.DataProvider = new AzureBlobDataProvider()
                {
                    ConnectionStringBuilder = blobConfig._eventConfig._activeDirectoryConfiguration._authConnectionStringBuilder,
                    ContainerNameBuilder = blobConfig._eventConfig._containerNameBuilder,
                    BlobNameBuilder = blobConfig._eventConfig._blobNameBuilder,
                    ResourceUrl = blobConfig._eventConfig._activeDirectoryConfiguration._resourceUrl,
                    TenantIdBuilder = blobConfig._eventConfig._activeDirectoryConfiguration._tenantIdBuilder,
                    UseActiveDirectory = true,
                    AccountNameBuilder = blobConfig._eventConfig._activeDirectoryConfiguration._accountNameBuilder,
                    EndpointSuffix = blobConfig._eventConfig._activeDirectoryConfiguration._endpointSuffix,
                    UseHttps = blobConfig._eventConfig._activeDirectoryConfiguration._useHttps
                };
                return new CreationPolicyConfigurator();

            }
            else
            {
                return UseAzureBlobStorage(configurator, blobConfig._eventConfig._connectionStringBuilder, blobConfig._eventConfig._containerNameBuilder, blobConfig._eventConfig._blobNameBuilder);
            }
        }
    }
}
