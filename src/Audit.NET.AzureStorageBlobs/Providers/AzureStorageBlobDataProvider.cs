using Audit.Core;
using System;
using System.Threading.Tasks;
#if NETSTANDARD2_0
using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using Azure.Storage.Blobs;
using Audit.AzureStorageBlobs.ConfigurationApi;
using Azure.Storage;
using Azure;
using Azure.Core;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Azure.Storage.Blobs.Models;

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

#if NETSTANDARD2_0
        public JsonSerializerOptions JsonSettings { get; set; } = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        public override object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string)
            {
                return value;
            }
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(value, JsonSettings), value.GetType(), JsonSettings);
        }
#endif

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

        private async Task<BlobContainerClient> EnsureContainerClientAsync(string containerName)
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
            await containerClient.CreateIfNotExistsAsync();
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
#if NETSTANDARD2_0
            blob.Upload(new BinaryData(auditEvent, JsonSettings), options);
#else
            blob.Upload(new BinaryData(auditEvent, Core.Configuration.JsonSettings), options);
#endif
            return blobName;
        }

        private async Task<string> UploadAsync(BlobContainerClient client, AuditEvent auditEvent, string existingBlobName)
        {
            var blobName = existingBlobName ?? BlobNameBuilder?.Invoke(auditEvent) ?? string.Format("{0}.json", Guid.NewGuid());
            var blob = client.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                Metadata = MetadataBuilder?.Invoke(auditEvent),
                AccessTier = AccessTierBuilder?.Invoke(auditEvent)
            };
#if NETSTANDARD2_0
            await blob.UploadAsync(new BinaryData(auditEvent, JsonSettings), options);
#else
            await blob.UploadAsync(new BinaryData(auditEvent, Core.Configuration.JsonSettings), options);
#endif
            return blobName;
        }

        private static List<BlobHierarchyItem> GetBlobHierarchyItems(BlobContainerClient containerClient, string prefix, string delimiter = "/")
        {
            List<BlobHierarchyItem> items = containerClient.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, delimiter, 
                prefix).ToList();

            var theList = new List<BlobHierarchyItem>();

            foreach (BlobHierarchyItem b in items)
            {
                if (b.IsBlob)
                {
                    theList.Add(b);
                }
                else
                {
                    theList.AddRange(GetBlobHierarchyItems(containerClient, b.Prefix));
                }
            }
            return theList;
        }

#region Public

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = EnsureContainerClient(containerName);
            var blobName = Upload(client, auditEvent, null);
            return blobName;
        }

        public async override Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = await EnsureContainerClientAsync(containerName);
            var blobName = await UploadAsync(client, auditEvent, null);
            return blobName;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = EnsureContainerClient(containerName);
            Upload(client, auditEvent, eventId.ToString());
        }
        public async override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var containerName = ContainerNameBuilder.Invoke(auditEvent);
            var client = await EnsureContainerClientAsync(containerName);
            await UploadAsync(client, auditEvent, eventId.ToString());
        }

        public override T GetEvent<T>(object blobName)
        {
            var containerName = ContainerNameBuilder.Invoke(null);
            return GetEvent<T>(containerName, blobName.ToString());
        }

        public async override Task<T> GetEventAsync<T>(object blobName)
        {
            var containerName = ContainerNameBuilder.Invoke(null);
            return await GetEventAsync<T>(containerName, blobName.ToString());
        }

        public T GetEvent<T>(string containerName, string blobName)
        {
            var client = EnsureContainerClient(containerName);
            var blobClient = client.GetBlobClient(blobName);
            if (blobClient.Exists())
            {
                var result = blobClient.DownloadContent();
#if NETSTANDARD2_0
                return result.Value.Content.ToObjectFromJson<T>(JsonSettings);
#else
                return result.Value.Content.ToObjectFromJson<T>(Core.Configuration.JsonSettings);
#endif
            }
            return default;
        }

        public async Task<T> GetEventAsync<T>(string containerName, string blobName)
        {
            var client = await EnsureContainerClientAsync(containerName);
            var blobClient = client.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync())
            {
                var result = await blobClient.DownloadContentAsync();
#if NETSTANDARD2_0
                return result.Value.Content.ToObjectFromJson<T>(JsonSettings);
#else
                return result.Value.Content.ToObjectFromJson<T>(Core.Configuration.JsonSettings);
#endif
            }
            return default;
        }

        public async Task<List<BlobHierarchyItem>> SearchEventsAsync(string prefix, string delimiter = "/")
        {
            var blobHierarchyItems = new List<BlobHierarchyItem>();
            if (string.IsNullOrEmpty(prefix)) return blobHierarchyItems;

            var containerName = ContainerNameBuilder.Invoke(null);
            BlobContainerClient client = await EnsureContainerClientAsync(containerName);
            blobHierarchyItems = GetBlobHierarchyItems(client, prefix, delimiter);

            return blobHierarchyItems;
        }

        public List<BlobHierarchyItem> SearchEvents(string prefix, string delimiter = "/")
        {
            var blobHierarchyItems = new List<BlobHierarchyItem>();
            if (string.IsNullOrEmpty(prefix)) return blobHierarchyItems;

            var containerName = ContainerNameBuilder.Invoke(null);
            BlobContainerClient client = EnsureContainerClient(containerName);
            blobHierarchyItems = GetBlobHierarchyItems(client, prefix, delimiter);

            return blobHierarchyItems;
        }
#endregion
    }
}
