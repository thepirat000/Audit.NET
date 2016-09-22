using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    /// <summary>
    /// Azure Blob Provider Configurator
    /// </summary>
    public interface IAzureBlobProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Storage connection string
        /// </summary>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        IAzureBlobProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the container name (must be lower case)
        /// </summary>
        /// <param name="containerName">The container name (must be lower case).</param>
        IAzureBlobProviderConfigurator ContainerName(string containerName);
        /// <summary>
        /// Specifies a function that returns the unique blob name for an event (can contain folders)
        /// </summary>
        /// <param name="blobNameBuilder">A function that returns the unique blob name for an event (can contain folders).</param>
        IAzureBlobProviderConfigurator BlobNameBuilder(Func<AuditEvent, string> blobNameBuilder);
    }
}