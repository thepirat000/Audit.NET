using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureBlobProviderConfigurator : IAzureBlobProviderConfigurator
    {
        internal Func<AuditEvent, string> _blobNameBuilder = null;
        internal Func<AuditEvent, string> _containerNameBuilder = null;
        internal Func<AuditEvent, string> _connectionStringBuilder = null;

        public IAzureBlobProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionStringBuilder = ev => connectionString;
            return this;
        }

        public IAzureBlobProviderConfigurator ContainerName(string containerName)
        {
            _containerNameBuilder = ev => containerName;
            return this;
        }

        public IAzureBlobProviderConfigurator ConnectionStringBuilder(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
            return this;
        }

        public IAzureBlobProviderConfigurator BlobNameBuilder(Func<AuditEvent, string> blobNamebuilder)
        {
            _blobNameBuilder = blobNamebuilder;
            return this;
        }

        public IAzureBlobProviderConfigurator ContainerNameBuilder(Func<AuditEvent, string> containerNameBuilder)
        {
            _containerNameBuilder = containerNameBuilder;
            return this;
        }
    }
}