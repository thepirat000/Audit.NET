using Audit.AzureTableStorage.ConfigurationApi;
using Audit.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.AzureTableStorage.Providers
{
    public class AzureBlobDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Value indicating whether to use Azure Active Directory for the connections to the storage service.
        /// </summary>
        public bool UseActiveDirectory { get; set; }
        /// <summary>
        /// The resource URL when using Azure AD. Default is "https://storage.azure.com/".
        /// </summary>
        public string ResourceUrl { get; set; } = "https://storage.azure.com/";
        /// <summary>
        /// The Tenant ID (Directory ID) to use for Azure AD
        /// </summary>
        public Func<AuditEvent, string> TenantIdBuilder { get; set; }
        /// <summary>
        /// The storage Account Name to use for Azure AD
        /// </summary>
        public Func<AuditEvent, string> AccountNameBuilder { get; set; }
        /// <summary>
        /// The DNS endpoint suffix to use when connecting to azure storage services. Default is "core.windows.net".
        /// </summary>
        public string EndpointSuffix { get; set; } = "core.windows.net";
        /// <summary>
        /// Value indicating whether to use HTTPS for Azure AD
        /// </summary>
        public bool UseHttps { get; set; } = true;

        private static readonly IDictionary<string, CloudBlobContainer> ContainerCache = new Dictionary<string, CloudBlobContainer>();

        /// <summary>
        /// Gets or sets a function that returns a unique name for the blob (can contain folders).
        /// </summary>
        public Func<AuditEvent, string> BlobNameBuilder { get; set; }

        /// <summary>
        /// Gets or sets a function that returns a container name for the event.
        /// </summary>
        public Func<AuditEvent, string> ContainerNameBuilder { get; set; }

        /// <summary>
        /// Gets or sets the container name to use.
        /// </summary>
        public string ContainerName { set { ContainerNameBuilder = _ => value; } }

        /// <summary>
        /// Gets or sets the Azure Storage connection string 
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder { get; set; }

        /// <summary>
        /// Sets the Azure Storage connection string
        /// </summary>
        public string ConnectionString { set { ConnectionStringBuilder = _ => value; } }

        public AzureBlobDataProvider()
        {
        }

        public AzureBlobDataProvider(Action<IAzureBlobProviderConfigurator> config)
        {
            var azConfig = new AzureBlobProviderConfigurator();
            if (config != null)
            {
                config.Invoke(azConfig);
                UseActiveDirectory = azConfig._eventConfig._useActiveDirectory;
                BlobNameBuilder = azConfig._eventConfig._blobNameBuilder;
                ContainerNameBuilder = azConfig._eventConfig._containerNameBuilder;
                ConnectionStringBuilder = azConfig._eventConfig._connectionStringBuilder;
                if (UseActiveDirectory)
                {
                    ResourceUrl = azConfig._eventConfig._activeDirectoryConfiguration._resourceUrl;
                    TenantIdBuilder = azConfig._eventConfig._activeDirectoryConfiguration._tenantIdBuilder;
                }
            }
        }

        #region Overrides
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var name = GetBlobName(auditEvent);
            Upload(name, auditEvent);
            return name;
        }

        public override T GetEvent<T>(object eventId)
        {
            var name = eventId.ToString();
            var container = EnsureContainer(null);
            var blob = container.GetBlockBlobReference(name);
            var json = blob.DownloadTextAsync().Result;
            return AuditEvent.FromJson<T>(json);
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var name = GetBlobName(auditEvent);
            await UploadAsync(name, auditEvent);
            return name;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var name = eventId as string;
            Upload(name, auditEvent);
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var name = eventId as string;
            await UploadAsync(name, auditEvent);
        }

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            var name = eventId.ToString();
            var container = await EnsureContainerAsync(null);
            var blob = container.GetBlockBlobReference(name);
            var json = await blob.DownloadTextAsync();
            return AuditEvent.FromJson<T>(json);
        }

        #endregion

        #region Private Methods
        private void Upload(string name, AuditEvent auditEvent)
        {
            var container = EnsureContainer(auditEvent);
            var blob = container.GetBlockBlobReference(name);
            var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            blob.UploadTextAsync(json).Wait();
        }

        private async Task UploadAsync(string name, AuditEvent auditEvent)
        {
            var container = await EnsureContainerAsync(auditEvent);
            var blob = container.GetBlockBlobReference(name);
            var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            await blob.UploadTextAsync(json);
        }

        protected string GetBlobName(AuditEvent auditEvent)
        {
            if (BlobNameBuilder != null)
            {
                return BlobNameBuilder.Invoke(auditEvent);
            }
            return string.Format("{0}.json", Guid.NewGuid());
        }

        protected string GetContainerName(AuditEvent auditEvent)
        {
            if (ContainerNameBuilder != null)
            {
                return ContainerNameBuilder.Invoke(auditEvent);
            }
            return "event";
        }

        protected string GetConnectionString(AuditEvent auditEvent)
        {
            return ConnectionStringBuilder?.Invoke(auditEvent);
        }

        protected string GetTenantId(AuditEvent auditEvent)
        {
            return TenantIdBuilder?.Invoke(auditEvent);
        }

        protected string GetAccountName(AuditEvent auditEvent)
        {
            return AccountNameBuilder?.Invoke(auditEvent);
        }

        protected virtual async Task<string> GetAccessTokenAsync(AuditEvent auditEvent)
        {
            var authCnnString = GetConnectionString(auditEvent);
            var tenantId = GetTenantId(auditEvent);
            var provider = new AzureServiceTokenProvider(authCnnString);
            var token = await provider.GetAccessTokenAsync(ResourceUrl, tenantId);
            return token;
        }

        internal CloudBlobContainer EnsureContainer(AuditEvent auditEvent)
        {
            var containerName = GetContainerName(auditEvent);
            if (UseActiveDirectory)
            {
                var accountName = GetAccountName(auditEvent);
                var token = GetAccessTokenAsync(auditEvent).GetAwaiter().GetResult();
                return EnsureContainerActiveDirectory(accountName, token, containerName);
            }
            else
            {
                var cnnString = GetConnectionString(auditEvent);
                
                return EnsureContainer(cnnString, containerName);
            }
        }

        internal async Task<CloudBlobContainer> EnsureContainerAsync(AuditEvent auditEvent)
        {
            var containerName = GetContainerName(auditEvent);
            if (UseActiveDirectory)
            {
                var accountName = GetAccountName(auditEvent);
                var token = await GetAccessTokenAsync(auditEvent);
                return await EnsureContainerActiveDirectoryAsync(accountName, token, containerName);
            }
            else
            {
                var cnnString = GetConnectionString(auditEvent);
                return await EnsureContainerAsync(cnnString, containerName);
            }
        }

        internal CloudBlobContainer EnsureContainer(string cnnString, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = cnnString + "|" + containerName;
            if (ContainerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();
            ContainerCache[cacheKey] = container;
            return container;
        }

        internal async Task<CloudBlobContainer> EnsureContainerAsync(string cnnString, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = cnnString + "|" + containerName;
            if (ContainerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            ContainerCache[cacheKey] = container;
            return container;
        }

        internal CloudBlobContainer EnsureContainerActiveDirectory(string accountName, string token, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = accountName + "." + EndpointSuffix + "|" + containerName + "|" + token;
            if (ContainerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var tokenCredential = new TokenCredential(token);
            var storageCredentials = new StorageCredentials(tokenCredential);
            var storageAccount = new CloudStorageAccount(storageCredentials, accountName, EndpointSuffix, UseHttps);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();
            ContainerCache[cacheKey] = container;
            return container;
        }

        internal async Task<CloudBlobContainer> EnsureContainerActiveDirectoryAsync(string accountName, string token, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = accountName + "." + EndpointSuffix + "|" + containerName + "|" + token;
            if (ContainerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var tokenCredential = new TokenCredential(token);
            var storageCredentials = new StorageCredentials(tokenCredential);
            var storageAccount = new CloudStorageAccount(storageCredentials, accountName, EndpointSuffix, UseHttps);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            ContainerCache[cacheKey] = container;
            return container;
        }

        #endregion
    }
}
