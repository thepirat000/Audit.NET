using Audit.Core;
using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Audit.AzureStorageBlobs.ConfigurationApi;
using Azure.Storage;
using Azure;
using Azure.Core;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Azure.Storage.Blobs.Models;
using System.Threading;

namespace Audit.AzureStorageBlobs.Providers
{
    public class AzureStorageBlobDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Azure Blob connection string
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Azure Blob Container name builder
        /// </summary>
        public Func<AuditEvent, string> ContainerNameBuilder { get; set; }
        /// <summary>
        /// Azure Blob name builder
        /// </summary>
        public Func<AuditEvent, string> BlobNameBuilder { get; set; }
        /// <summary>
        /// The Azure.Storage.Blobs Client Options to use
        /// </summary>
        public BlobClientOptions ClientOptions { get; set; }
        /// <summary>
        /// The Service URL to connect to. Alternative to ConnectionString.
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// The Shared Key credential to use to connect to the Service URL.
        /// </summary>
        public StorageSharedKeyCredential SharedKeyCredential { get; set; }
        /// <summary>
        /// The Sas credential to use to connect to the Service URL.
        /// </summary>
        public AzureSasCredential SasCredential { get; set; }
        /// <summary>
        /// The Token credential to use to connect to the Service URL.
        /// </summary>
        public TokenCredential TokenCredential { get; set; }
        /// <summary>
        /// Gets or sets a function that returns the standard blob tier to use (or null to use the default).
        /// </summary>
        public Func<AuditEvent, AccessTier?> AccessTierBuilder { get; set; }
        /// <summary>
        /// Gets or sets a function that returns the metadata key/values to store on the blob.
        /// </summary>
        public Func<AuditEvent, IDictionary<string, string>> MetadataBuilder { get; set; }

        private static readonly IDictionary<string, BlobContainerClient> ContainerClientCache = new ConcurrentDictionary<string, BlobContainerClient>();

        public AzureStorageBlobDataProvider()
        {
        }

        public AzureStorageBlobDataProvider(Action<IAzureBlobConnectionConfigurator> config)
        {
            var cfg = new AzureBlobConnectionConfigurator();
            config.Invoke(cfg);

            ConnectionString = cfg._connectionString;
            ContainerNameBuilder = cfg._containerConfig._containerNameBuilder;
            BlobNameBuilder = cfg._containerConfig._blobNameBuilder;
            ClientOptions = cfg._containerConfig._clientOptions;
            AccessTierBuilder = cfg._containerConfig._accessTierBuilder;
            MetadataBuilder = cfg._containerConfig._metadataBuilder;
            if (cfg._credentialConfig != null)
            {
                ServiceUrl = cfg._credentialConfig._serviceUrl;
                SharedKeyCredential = cfg._credentialConfig._sharedKeyCredential;
                SasCredential = cfg._credentialConfig._sasCredential;
                TokenCredential = cfg._credentialConfig._tokenCredential;
            }
        }

        private BlobContainerClient EnsureContainerClient(string containerName)
        {
            var cacheKey = $"{ConnectionString ?? ServiceUrl}|{containerName}";
            if (ContainerClientCache.TryGetValue(cacheKey, out BlobContainerClient result))
            {
                // Cache hit
                return result;
            }
            // Cache miss
            var serviceClient = CreateBlobServiceClient();
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists();
            ContainerClientCache[cacheKey] = containerClient;
            return containerClient;
        }

        private async Task<BlobContainerClient> EnsureContainerClientAsync(string containerName, CancellationToken cancellationToken)
        {
            var cacheKey = $"{ConnectionString ?? ServiceUrl}|{containerName}";
            if (ContainerClientCache.TryGetValue(cacheKey, out BlobContainerClient result))
            {
                // Cache hit
                return result;
            }
            // Cache miss
            var serviceClient = CreateBlobServiceClient();
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(default, default, default, cancellationToken);
            ContainerClientCache[cacheKey] = containerClient;
            return containerClient;
        }

        private BlobServiceClient CreateBlobServiceClient()
        {
            BlobServiceClient serviceClient;
            if (ConnectionString != null)
            {
                serviceClient = new BlobServiceClient(ConnectionString, ClientOptions);
            }
            else
            {
                if (SasCredential != null)
                {
                    // Using SAS credential 
                    serviceClient = new BlobServiceClient(new Uri(ServiceUrl), SasCredential, ClientOptions);
                }
                else if (SharedKeyCredential != null)
                {
                    // Using Shared Key credential 
                    serviceClient = new BlobServiceClient(new Uri(ServiceUrl), SharedKeyCredential, ClientOptions);
                }
                else if (TokenCredential != null)
                {
                    // Using Token Credential
                    serviceClient = new BlobServiceClient(new Uri(ServiceUrl), TokenCredential, ClientOptions);
                }
                else
                {
                    // Anonymous by service URL
                    serviceClient = new BlobServiceClient(new Uri(ServiceUrl), ClientOptions);
                }
            }
            return serviceClient;
        }

        private string Upload(BlobContainerClient client, AuditEvent auditEvent, string existingBlobName)
        {
            var blobName = existingBlobName ?? BlobNameBuilder?.Invoke(auditEvent) ?? string.Format("{0}.json", Guid.NewGuid());
            var blob = client.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                Metadata = MetadataBuilder?.Invoke(auditEvent),
                AccessTier = AccessTierBuilder?.Invoke(auditEvent)
            };
            blob.Upload(new BinaryData(auditEvent, Configuration.JsonSettings), options);
            
            return blobName;
        }

        private async Task<string> UploadAsync(BlobContainerClient client, AuditEvent auditEvent, string existingBlobName, CancellationToken cancellationToken)
        {
            var blobName = existingBlobName ?? BlobNameBuilder?.Invoke(auditEvent) ?? string.Format("{0}.json", Guid.NewGuid());
            var blob = client.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                Metadata = MetadataBuilder?.Invoke(auditEvent),
                AccessTier = AccessTierBuilder?.Invoke(auditEvent)
            };
            await blob.UploadAsync(new BinaryData(auditEvent, Core.Configuration.JsonSettings), options, cancellationToken);

            return blobName;
        }

#region Public

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = EnsureContainerClient(containerName);
            var blobName = Upload(client, auditEvent, null);
            return blobName;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = await EnsureContainerClientAsync(containerName, cancellationToken);
            var blobName = await UploadAsync(client, auditEvent, null, cancellationToken);
            return blobName;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = EnsureContainerClient(containerName);
            Upload(client, auditEvent, eventId.ToString());
        }
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = await EnsureContainerClientAsync(containerName, cancellationToken);
            await UploadAsync(client, auditEvent, eventId.ToString(), cancellationToken);
        }

        public override T GetEvent<T>(object blobName)
        {
            var containerName = ContainerNameBuilder.Invoke(null);
            return GetEvent<T>(containerName, blobName.ToString());
        }

        public override async Task<T> GetEventAsync<T>(object blobName, CancellationToken cancellationToken = default)
        {
            var containerName = ContainerNameBuilder.Invoke(null);
            return await GetEventAsync<T>(containerName, blobName.ToString(), cancellationToken);
        }

        public T GetEvent<T>(string containerName, string blobName)
        {
            var client = EnsureContainerClient(containerName);
            var blobClient = client.GetBlobClient(blobName);
            if (blobClient.Exists())
            {
                var result = blobClient.DownloadContent();
                
                return result.Value.Content.ToObjectFromJson<T>(Core.Configuration.JsonSettings);
            }
            return default;
        }

        public async Task<T> GetEventAsync<T>(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            var client = await EnsureContainerClientAsync(containerName, cancellationToken);
            var blobClient = client.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync(cancellationToken))
            {
                var result = await blobClient.DownloadContentAsync(cancellationToken);

                return result.Value.Content.ToObjectFromJson<T>(Core.Configuration.JsonSettings);
            }
            return default;
        }
#endregion
    }
}
