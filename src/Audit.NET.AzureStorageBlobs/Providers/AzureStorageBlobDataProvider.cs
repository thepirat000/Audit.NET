﻿using Audit.Core;
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
        /// Azure Blob Container name 
        /// </summary>
        public Setting<string> ContainerName { get; set; }
        /// <summary>
        /// Azure Blob name builder
        /// </summary>
        public Setting<string> BlobName { get; set; }
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
        /// Gets or sets the standard blob tier to use (or null to use the default).
        /// </summary>
        public Setting<AccessTier?> AccessTier { get; set; }
        /// <summary>
        /// Gets or sets the metadata key/values to store on the blob.
        /// </summary>
        public Setting<IDictionary<string, string>> Metadata { get; set; }
        /// <summary>
        /// Gets or sets the Tags to store on the blob.
        /// </summary>
        public Setting<IDictionary<string, string>> Tags { get; set; }

        private static readonly IDictionary<string, BlobContainerClient> ContainerClientCache = new ConcurrentDictionary<string, BlobContainerClient>();

        public AzureStorageBlobDataProvider()
        {
        }

        public AzureStorageBlobDataProvider(Action<IAzureBlobConnectionConfigurator> config)
        {
            var cfg = new AzureBlobConnectionConfigurator();
            config.Invoke(cfg);

            ConnectionString = cfg._connectionString;
            ContainerName = cfg._containerConfig._containerName;
            BlobName = cfg._containerConfig._blobName;
            ClientOptions = cfg._containerConfig._clientOptions;
            AccessTier = cfg._containerConfig._accessTier;
            Metadata = cfg._containerConfig._metadata;
            Tags = cfg._containerConfig._tags;
            if (cfg._credentialConfig != null)
            {
                ServiceUrl = cfg._credentialConfig._serviceUrl;
                SharedKeyCredential = cfg._credentialConfig._sharedKeyCredential;
                SasCredential = cfg._credentialConfig._sasCredential;
                TokenCredential = cfg._credentialConfig._tokenCredential;
            }
        }

        /// <summary>
        /// Returns the instance of the Azure.Storage.Blobs.BlobServiceClient to use for the given AuditEvent
        /// </summary>
        public BlobContainerClient GetContainerClient(AuditEvent auditEvent)
        {
            var containerName = ContainerName.GetValue(auditEvent);
            
            return EnsureContainerClient(containerName);
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

        /// <summary>
        /// Returns the instance of the Azure.Storage.Blobs.BlobServiceClient to use for the given AuditEvent
        /// </summary>
        public Task<BlobContainerClient> GetContainerClientAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            var containerName = ContainerName.GetValue(auditEvent);
            return EnsureContainerClientAsync(containerName, cancellationToken);
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

        protected string Upload(BlobContainerClient client, AuditEvent auditEvent, string existingBlobName)
        {
            var blobName = existingBlobName ?? BlobName.GetValue(auditEvent) ?? string.Format("{0}.json", Guid.NewGuid());
            var blob = client.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                Metadata = Metadata.GetValue(auditEvent),
                AccessTier = AccessTier.GetValue(auditEvent),
                Tags = Tags.GetValue(auditEvent)
                
            };
            blob.Upload(new BinaryData(auditEvent, Configuration.JsonSettings), options);
            
            return blobName;
        }

        protected async Task<string> UploadAsync(BlobContainerClient client, AuditEvent auditEvent, string existingBlobName, CancellationToken cancellationToken)
        {
            var blobName = existingBlobName ?? BlobName.GetValue(auditEvent) ?? string.Format("{0}.json", Guid.NewGuid());
            var blob = client.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                Metadata = Metadata.GetValue(auditEvent),
                AccessTier = AccessTier.GetValue(auditEvent),
                Tags = Tags.GetValue(auditEvent)
            };
            await blob.UploadAsync(new BinaryData(auditEvent, Core.Configuration.JsonSettings), options, cancellationToken);

            return blobName;
        }

#region Public

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetContainerClient(auditEvent);
            var blobName = Upload(client, auditEvent, null);
            return blobName;
        }

        /// <inheritdoc />
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var client = await GetContainerClientAsync(auditEvent, cancellationToken);
            var blobName = await UploadAsync(client, auditEvent, null, cancellationToken);
            return blobName;
        }

        /// <inheritdoc />
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var client = GetContainerClient(auditEvent);
            Upload(client, auditEvent, eventId.ToString());
        }
        
        /// <inheritdoc />
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var client = await GetContainerClientAsync(auditEvent, cancellationToken);
            await UploadAsync(client, auditEvent, eventId.ToString(), cancellationToken);
        }

        /// <inheritdoc />
        public override T GetEvent<T>(object eventId)
        {
            var containerName = ContainerName.GetDefault();
            return GetEvent<T>(containerName, eventId.ToString());
        }

        /// <inheritdoc />
        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var containerName = ContainerName.GetDefault();
            return await GetEventAsync<T>(containerName, eventId.ToString(), cancellationToken);
        }

        /// <summary>
        /// Get the event from the blob storage by container name and blob name
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Blob name</param>
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

        /// <summary>
        /// Get the event from the blob storage by container name and blob name
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Blob name</param>
        /// <param name="cancellationToken">The cancellation token</param>
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
