using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureBlobProviderEventConfigurator : IAzureBlobProviderEventConfigurator
    {
        internal Func<AuditEvent, string> _blobNameBuilder = null;
        internal Func<AuditEvent, string> _containerNameBuilder = null;

        internal Func<AuditEvent, string> _connectionStringBuilder = null;
        internal AzureActiveDirectoryConfigurator _activeDirectoryConfiguration = null;
        internal bool _useActiveDirectory = false;

        public IAzureBlobProviderEventConfigurator ContainerName(string containerName)
        {
            _containerNameBuilder = ev => containerName;
            return this;
        }

        public IAzureBlobProviderEventConfigurator ContainerNameBuilder(Func<AuditEvent, string> containerNameBuilder)
        {
            _containerNameBuilder = containerNameBuilder;
            return this;
        }

        public IAzureBlobProviderEventConfigurator BlobNameBuilder(Func<AuditEvent, string> blobNamebuilder)
        {
            _blobNameBuilder = blobNamebuilder;
            return this;
        }
    }
}