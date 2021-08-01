using System;
using Audit.Core.ConfigurationApi;
using Audit.AzureTableStorage.Providers;
using Audit.AzureTableStorage.ConfigurationApi;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;

namespace Audit.Core
{
    public static class AzureStorageConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Blob Storage.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="connectionStringBuilder">A builder that returns a connection string for an event.</param>
        /// <param name="containerNameBuilder">A builder that returns a container name for an event.</param>
        /// <param name="blobNameBuilder">A builder that returns a unique name for the blob (can contain folders).</param>
        /// <param name="accessTierBuilder">A builder that returns the Standard BLOB Access tier to use.</param>
        /// <param name="metadataBuilder">A builder that returns the metadata collection of key/values to store within the blob.</param>
        private static ICreationPolicyConfigurator UseAzureBlobStorage(this IConfigurator configurator, Func<AuditEvent, string> connectionStringBuilder = null,
            Func<AuditEvent, string> containerNameBuilder = null, Func<AuditEvent, string> blobNameBuilder = null, Func<AuditEvent, StandardBlobTier?> accessTierBuilder = null,
            Func<AuditEvent, IDictionary<string, string>> metadataBuilder = null)
        {
            Configuration.DataProvider = new AzureBlobDataProvider()
            {
                ConnectionStringBuilder = connectionStringBuilder,
                ContainerNameBuilder = containerNameBuilder,
                BlobNameBuilder = blobNameBuilder,
                AccessTierBuilder = accessTierBuilder,
                MetadataBuilder = metadataBuilder
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
                    UseHttps = blobConfig._eventConfig._activeDirectoryConfiguration._useHttps,
                    AccessTierBuilder = blobConfig._eventConfig._accessTierBuilder,
                    MetadataBuilder = blobConfig._eventConfig._metadataBuilder
                };
                return new CreationPolicyConfigurator();
            }
            else
            {
                return UseAzureBlobStorage(configurator, blobConfig._eventConfig._connectionStringBuilder, blobConfig._eventConfig._containerNameBuilder, blobConfig._eventConfig._blobNameBuilder, blobConfig._eventConfig._accessTierBuilder, blobConfig._eventConfig._metadataBuilder);
            }
        }
    }
}
