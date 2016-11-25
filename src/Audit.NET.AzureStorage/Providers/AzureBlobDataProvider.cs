using Audit.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

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

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var name = eventId as string;
            Upload(name, auditEvent);
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
            CloudBlobContainer result;
            var containerName = GetContainerName(auditEvent);
            var cnnString = GetConnectionString(auditEvent);
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
        #endregion
    }
}
