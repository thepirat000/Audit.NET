using Audit.Core;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public class AzureTableConnectionConfigurator : IAzureTableConnectionConfigurator
    {
        internal Func<AuditEvent, TableClient> _clientFactory;

        internal string _connectionString;

        internal Uri _endpointUri;
        internal TableSharedKeyCredential _sharedKeyCredential;
        internal AzureSasCredential _sasCredential;
        internal TokenCredential _tokenCredential;

        internal AzureTableEntityConfigurator _tableConfig = new AzureTableEntityConfigurator();

        public IAzureTablesEntityConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return _tableConfig;
        }

        public IAzureTablesEntityConfigurator Endpoint(Uri endpoint)
        {
            _endpointUri = endpoint;
            return _tableConfig;
        }

        public IAzureTablesEntityConfigurator Endpoint(Uri endpoint, TableSharedKeyCredential credential)
        {
            _endpointUri = endpoint;
            _sharedKeyCredential = credential;
            return _tableConfig;
        }

        public IAzureTablesEntityConfigurator Endpoint(Uri endpoint, AzureSasCredential credential)
        {
            _endpointUri = endpoint;
            _sasCredential = credential;
            return _tableConfig;
        }

        public IAzureTablesEntityConfigurator Endpoint(Uri endpoint, TokenCredential credential)
        {
            _endpointUri = endpoint;
            _tokenCredential = credential;
            return _tableConfig;
        }

        public IAzureTablesEntityConfigurator TableClientFactory(Func<AuditEvent, TableClient> clientFactory)
        {
            _clientFactory = clientFactory;
            return _tableConfig;
        }
    }
}
