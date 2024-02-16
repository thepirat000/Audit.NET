using Audit.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{
    public class AzureBlobContainerConfigurator : IAzureBlobContainerConfigurator
    {
        internal Setting<string> _blobName;
        internal Setting<string> _containerName;
        internal BlobClientOptions _clientOptions;
        internal Setting<AccessTier?> _accessTier;
        internal Setting<IDictionary<string, string>> _metadata;

        public IAzureBlobContainerConfigurator BlobName(Func<AuditEvent, string> blobNameBuilder)
        {
            _blobName = blobNameBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator ClientOptions(BlobClientOptions options)
        {
            _clientOptions = options;
            return this;
        }

        public IAzureBlobContainerConfigurator ContainerName(string containerName)
        {
            _containerName = containerName;
            return this;
        }

        public IAzureBlobContainerConfigurator ContainerName(Func<AuditEvent, string> containerNameBuilder)
        {
            _containerName = containerNameBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator AccessTier(AccessTier accessTier)
        {
            _accessTier = accessTier;
            return this;
        }

        public IAzureBlobContainerConfigurator AccessTier(Func<AuditEvent, AccessTier?> accessTierBuilder)
        {
            _accessTier = accessTierBuilder;
            return this;
        }

        public IAzureBlobContainerConfigurator Metadata(Func<AuditEvent, IDictionary<string, string>> metadataBuilder)
        {
            _metadata = metadataBuilder;
            return this;
        }
    }
}
