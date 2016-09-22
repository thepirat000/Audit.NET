using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureBlobProviderConfigurator : IAzureBlobProviderConfigurator
    {
        internal Func<AuditEvent, string> _blobNameBuilder = null;
        internal string _connectionString = null;
        internal string _containerName = "event";

        public IAzureBlobProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public IAzureBlobProviderConfigurator ContainerName(string containerName)
        {
            _containerName = containerName;
            return this;
        }

        public IAzureBlobProviderConfigurator BlobNameBuilder(Func<AuditEvent, string> blobNamebuilder)
        {
            _blobNameBuilder = blobNamebuilder;
            return this;
        }
    }
}