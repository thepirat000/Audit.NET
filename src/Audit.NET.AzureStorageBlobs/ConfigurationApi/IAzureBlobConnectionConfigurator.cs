using System;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{
    public interface IAzureBlobConnectionConfigurator
    {
        /// <summary>
        /// Setup the Azure Blob connection with a connection string
        /// </summary>
        IAzureBlobContainerConfigurator WithConnectionString(string connectionString);
        /// <summary>
        /// Setup the Azure Blob connection with a service URL and no credentials (anonymous)
        /// </summary>
        IAzureBlobContainerConfigurator WithServiceUrl(string serviceUrl);
        /// <summary>
        /// Setup the Azure Blob connection with a service URL and given credentials (StorageSharedKeyCredential, AzureSasCredential or TokenCredential)
        /// </summary>
        IAzureBlobContainerConfigurator WithCredentials(Action<IAzureBlobCredentialConfiguration> credentialConfig);
    }
}
