using Audit.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{
    public class AzureBlobContainerConfigurator : IAzureBlobContainerConfigurator
    {
        internal Func<AuditEvent, string> _blobNameBuilder;
        internal Func<AuditEvent, string> _containerNameBuilder;
        internal BlobClientOptions _clientOptions;
        internal Func<AuditEvent, AccessTier?> _accessTierBuilder;
        internal Func<AuditEvent, IDictionary<string, string>> _metadataBuilder;

        public IAzureBlobContainerConfigurator BlobName(Func<AuditEvent, string> blobNameBuilder)
        {
            _blobNameBuilder = blobNameBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator ClientOptions(BlobClientOptions options)
        {
            _clientOptions = options;
            return this;
        }

        public IAzureBlobContainerConfigurator ContainerName(string containerName)
        {
            _containerNameBuilder = _ => containerName;
            return this;
        }

        public IAzureBlobContainerConfigurator ContainerName(Func<AuditEvent, string> containerNameBuilder)
        {
            _containerNameBuilder = containerNameBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator AccessTier(AccessTier accessTier)
        {
            _accessTierBuilder = _ => accessTier;
            return this;
        }

        public IAzureBlobContainerConfigurator AccessTier(Func<AuditEvent, AccessTier?> accessTierBuilder)
        {
            _accessTierBuilder = accessTierBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator Metadata(Func<AuditEvent, IDictionary<string, string>> metadataBuilder)
        {
            _metadataBuilder = metadataBuilder;
            return this;
        }

    }
}
