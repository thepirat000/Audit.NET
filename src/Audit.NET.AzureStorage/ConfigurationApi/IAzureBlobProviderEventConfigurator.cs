using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public interface IAzureBlobProviderEventConfigurator
    {
        /// <summary>
        /// Specifies a function that returns the unique blob name for an event (can contain folders)
        /// </summary>
        /// <param name="blobNameBuilder">A function that returns the unique blob name for an event (can contain folders).</param>
        IAzureBlobProviderEventConfigurator BlobNameBuilder(Func<AuditEvent, string> blobNameBuilder);
        /// <summary>
        /// Specifies the container name (must be lower case)
        /// </summary>
        /// <param name="containerName">The container name (must be lower case).</param>
        IAzureBlobProviderEventConfigurator ContainerName(string containerName);
        /// <summary>
        /// Specifies a function that returns the container name to use for an event
        /// </summary>
        /// <param name="containerNameBuilder">A function that returns the container name for an event.</param>
        IAzureBlobProviderEventConfigurator ContainerNameBuilder(Func<AuditEvent, string> containerNameBuilder);
    }
}