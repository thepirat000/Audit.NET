using System;
using System.Collections.Generic;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public interface IAzureBlobProviderEventConfigurator
    {
        /// <summary>
        /// Specifies a function that returns the unique blob name for an event (can contain folders)
        /// </summary>
        /// <param name="blobNameBuilder">A function that returns the unique blob name for an event (can contain folders).</param>
        IAzureBlobProviderEventConfigurator BlobName(Func<AuditEvent, string> blobNameBuilder);
        /// <summary>
        /// Specifies the container name (must be lower case)
        /// </summary>
        /// <param name="containerName">The container name (must be lower case).</param>
        IAzureBlobProviderEventConfigurator ContainerName(string containerName);
        /// <summary>
        /// Specifies a function that returns the container name to use for an event
        /// </summary>
        /// <param name="containerNameBuilder">A function that returns the container name for an event.</param>
        IAzureBlobProviderEventConfigurator ContainerName(Func<AuditEvent, string> containerNameBuilder);
        /// <summary>
        /// Sets the Standard BLOB Access Tier to use for all the audit events
        /// </summary>
        IAzureBlobProviderEventConfigurator WithAccessTier(StandardBlobTier accessTier);
        /// <summary>
        /// Sets the Standard BLOB Access Tier to use for the given audit event
        /// </summary>
        IAzureBlobProviderEventConfigurator WithAccessTier(Func<AuditEvent, StandardBlobTier?> accessTierBuilder);
        /// <summary>
        /// Sets the User-defined metadata as a collection of name-value pairs related to the resource
        /// </summary>
        IAzureBlobProviderEventConfigurator WithMetadata(Func<AuditEvent, IDictionary<string, string>> metadataBuilder);
    }
}