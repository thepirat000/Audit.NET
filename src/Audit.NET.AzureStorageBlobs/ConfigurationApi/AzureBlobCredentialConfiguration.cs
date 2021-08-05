using Audit.Core;
using Azure;
using Azure.Core;
using Azure.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.AzureStorageBlobs.ConfigurationApi
{

    public class AzureBlobCredentialConfiguration : IAzureBlobCredentialConfiguration
    {
        internal string _serviceUrl;
        internal StorageSharedKeyCredential _sharedKeyCredential;
        internal AzureSasCredential _sasCredential;
        internal TokenCredential _tokenCredential;

        public IAzureBlobCredentialConfiguration Url(string url)
        {
            _serviceUrl = url;
            return this;
        }

        public IAzureBlobCredentialConfiguration Credential(StorageSharedKeyCredential credential)
        {
            _sharedKeyCredential = credential;
            return this;
        }

        public IAzureBlobCredentialConfiguration Credential(AzureSasCredential credential)
        {
            _sasCredential = credential;
            return this;
        }

        public IAzureBlobCredentialConfiguration Credential(TokenCredential credential)
        {
            _tokenCredential = credential;
            return this;
        }
    }
}
