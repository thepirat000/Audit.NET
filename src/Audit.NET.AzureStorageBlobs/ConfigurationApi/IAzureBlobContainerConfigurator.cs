using Audit.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{
    public interface IAzureBlobContainerConfigurator
    {
        /// <summary>
        /// Sets the blob container name to use
        /// </summary>
        IAzureBlobContainerConfigurator ContainerName(string containerName);
        /// <summary>
        /// Sets the blob container name to use as a function of the audit event
        /// </summary>
        IAzureBlobContainerConfigurator ContainerName(Func<AuditEvent, string> containerNameBuilder);
        /// <summary>
        /// Sets the client options to use
        /// </summary>
        IAzureBlobContainerConfigurator ClientOptions(BlobClientOptions options);
        /// <summary>
        /// Sets the unique blob name to use as a function of the audit event
        /// </summary>
        IAzureBlobContainerConfigurator BlobName(Func<AuditEvent, string> blobNameBuilder);
        /// <summary>
        /// Sets the access tier to use (optional)
        /// </summary>
        IAzureBlobContainerConfigurator AccessTier(AccessTier accessTier);
        /// <summary>
        /// Sets the access tier to use as a function of the audit event (optional)
        /// </summary>
        IAzureBlobContainerConfigurator AccessTier(Func<AuditEvent, AccessTier?> accessTierBuilder);
        /// <summary>
        /// Sets the metadata to associate to the given audit event blob (optional)
        /// </summary>
        IAzureBlobContainerConfigurator Metadata(Func<AuditEvent, IDictionary<string, string>> metadataBuilder);
    }
}
