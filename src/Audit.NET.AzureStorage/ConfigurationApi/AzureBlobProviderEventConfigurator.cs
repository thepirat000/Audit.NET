using System;
using System.Collections.Generic;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureBlobProviderEventConfigurator : IAzureBlobProviderEventConfigurator
    {
        internal Func<AuditEvent, string> _blobNameBuilder = null;
        internal Func<AuditEvent, string> _containerNameBuilder = null;
        internal Func<AuditEvent, string> _connectionStringBuilder = null;
        internal AzureActiveDirectoryConfigurator _activeDirectoryConfiguration = null;
        internal bool _useActiveDirectory = false;
        internal Func<AuditEvent, StandardBlobTier?> _accessTierBuilder = null;
        internal Func<AuditEvent, IDictionary<string, string>> _metadataBuilder = null;

        public IAzureBlobProviderEventConfigurator ContainerName(string containerName)
        {
            _containerNameBuilder = ev => containerName;
            return this;
        }

        public IAzureBlobProviderEventConfigurator ContainerName(Func<AuditEvent, string> containerNameBuilder)
        {
            _containerNameBuilder = containerNameBuilder;
            return this;
        }

        public IAzureBlobProviderEventConfigurator BlobName(Func<AuditEvent, string> blobNamebuilder)
        {
            _blobNameBuilder = blobNamebuilder;
            return this;
        }
        
        public IAzureBlobProviderEventConfigurator WithAccessTier(StandardBlobTier accessTier)
        {
            _accessTierBuilder = ev => accessTier;
            return this;
        }

        public IAzureBlobProviderEventConfigurator WithAccessTier(Func<AuditEvent, StandardBlobTier?> accessTierBuilder)
        {
            _accessTierBuilder = accessTierBuilder;
            return this;
        }

        public IAzureBlobProviderEventConfigurator WithMetadata(Func<AuditEvent, IDictionary<string, string>> metadataBuilder)
        {
            _metadataBuilder = metadataBuilder;
            return this;
        }
    }
}