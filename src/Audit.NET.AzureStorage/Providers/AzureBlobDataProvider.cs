using Audit.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;

namespace Audit.AzureTableStorage.Providers
{
    public class AzureBlobDataProvider : AuditDataProvider
    {
        private CloudBlobContainer _container;
        private string _connectionString;
        private Func<AuditEvent, string> _blobNameBuilder = null;
        private string _containerName = string.Empty;

        /// <summary>
        /// Gets or sets a function that returns a unique name for the blob (can contain folders).
        /// </summary>
        public Func<AuditEvent, string> BlobNameBuilder
        {
            get { return _blobNameBuilder; }
            set { _blobNameBuilder = value; }
        }

        /// <summary>
        /// Gets or sets the Azure Storage container name
        /// </summary>
        public string ContainerName
        {
            get { return _containerName; }
            set { _containerName = value; }
        }

        /// <summary>
        /// Gets or sets the Azure Storage connection string
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

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

        private void Upload(string name, AuditEvent auditEvent)
        {
            EnsureContainer();
            var blob = _container.GetBlockBlobReference(name);
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

        private void EnsureContainer()
        {
            if (_container == null)
            {
                var storageAccount = CloudStorageAccount.Parse(_connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(_containerName);
                container.CreateIfNotExistsAsync().Wait();
                _container = container;
            }
        }
    }
}
