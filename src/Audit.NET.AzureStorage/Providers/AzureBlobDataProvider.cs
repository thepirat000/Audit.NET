using Audit.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.AzureTableStorage.Providers
{
    public class AzureBlobDataProvider : AuditDataProvider
    {
        private static IDictionary<string, CloudBlobContainer> _containerCache = new Dictionary<string, CloudBlobContainer>();
        private Func<AuditEvent, string> _connectionStringBuilder = null;
        private Func<AuditEvent, string> _blobNameBuilder = null;
        private Func<AuditEvent, string> _containerNameBuilder = null;

        /// <summary>
        /// Gets or sets a function that returns a unique name for the blob (can contain folders).
        /// </summary>
        public Func<AuditEvent, string> BlobNameBuilder
        {
            get { return _blobNameBuilder; }
            set { _blobNameBuilder = value; }
        }

        /// <summary>
        /// Gets or sets a function that returns a container name for the event.
        /// </summary>
        public Func<AuditEvent, string> ContainerNameBuilder
        {
            get { return _containerNameBuilder; }
            set { _containerNameBuilder = value; }
        }

        /// <summary>
        /// Gets or sets the Azure Storage connection string
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder
        {
            get { return _connectionStringBuilder; }
            set { _connectionStringBuilder = value; }
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

        private string GetBlobName(AuditEvent auditEvent)
        {
            if (_blobNameBuilder != null)
            {
                return _blobNameBuilder.Invoke(auditEvent);
            }
            return string.Format("{0}.json", Guid.NewGuid());
        }

        private string GetContainerName(AuditEvent auditEvent)
        {
            if (_containerNameBuilder != null)
            {
                return _containerNameBuilder.Invoke(auditEvent);
            }
            return "event";
        }

        private string GetConnectionString(AuditEvent auditEvent)
        {
            return _connectionStringBuilder?.Invoke(auditEvent);
        }

        internal CloudBlobContainer EnsureContainer(AuditEvent auditEvent)
        {
            var cnnString = GetConnectionString(auditEvent);
            var containerName = GetContainerName(auditEvent);
            return EnsureContainer(cnnString, containerName);
        }

        internal async Task<CloudBlobContainer> EnsureContainerAsync(AuditEvent auditEvent)
        {
            var cnnString = GetConnectionString(auditEvent);
            var containerName = GetContainerName(auditEvent);
            return await EnsureContainerAsync(cnnString, containerName);
        }

        internal CloudBlobContainer EnsureContainer(string cnnString, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = cnnString + "|" + containerName;
            if (_containerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();
            _containerCache[cacheKey] = container;
            return container;
        }

        internal async Task<CloudBlobContainer> EnsureContainerAsync(string cnnString, string containerName)
        {
            CloudBlobContainer result;
            var cacheKey = cnnString + "|" + containerName;
            if (_containerCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            _containerCache[cacheKey] = container;
            return container;
        }
        #endregion
    }
}
