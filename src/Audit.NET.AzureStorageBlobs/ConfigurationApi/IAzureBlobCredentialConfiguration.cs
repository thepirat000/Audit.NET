using Azure;
using Azure.Core;
using Azure.Storage;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{
    public interface IAzureBlobCredentialConfiguration
    {
        /// <summary>
        /// The azure storage service URL
        /// </summary>
        IAzureBlobCredentialConfiguration Url(string url);
        /// <summary>
        /// The credentials to authenticate
        /// </summary>
        IAzureBlobCredentialConfiguration Credential(StorageSharedKeyCredential credential);
        /// <summary>
        /// The credentials to authenticate
        /// </summary>
        IAzureBlobCredentialConfiguration Credential(AzureSasCredential credential);
        /// <summary>
        /// The credentials to authenticate
        /// </summary>
        IAzureBlobCredentialConfiguration Credential(TokenCredential credential);
    }
}
