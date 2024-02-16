using System;
using Audit.Core;
#if IS_COSMOS
using Microsoft.Azure.Cosmos;
#else
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
#endif

namespace Audit.AzureCosmos.ConfigurationApi
{
    public class AzureCosmosProviderConfigurator : IAzureCosmosProviderConfigurator
    {
        internal Setting<string> _endpoint = string.Empty;
        internal Setting<string> _authKey;
        internal Setting<string> _database = "Audit";
        internal Setting<string> _container = "Events";
        internal Func<AuditEvent, string> _idBuilder;
#if IS_COSMOS
        internal CosmosClient _cosmosClient;
        internal Action<CosmosClientOptions> _cosmosClientOptionsAction;
#else
        internal Setting<ConnectionPolicy> _connectionPolicy;
        internal IDocumentClient _documentClient = null;
#endif

        public IAzureCosmosProviderConfigurator Endpoint(Func<AuditEvent, string> endpointBuilder)
        {
            _endpoint = endpointBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Endpoint(string endpoint)
        {
            _endpoint = endpoint;
            return this;
        }
        
        public IAzureCosmosProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder)
        {
            _database = databaseBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(string database)
        {
            _database = database;
            return this;
        }

        public IAzureCosmosProviderConfigurator Container(Func<AuditEvent, string> containerBuilder)
        {
            _container = containerBuilder;
            return this;
        }
        
        public IAzureCosmosProviderConfigurator Container(string container)
        {
            _container = container;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(Func<AuditEvent, string> authKeyBuilder)
        {
            _authKey = authKeyBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(string authKey)
        {
            _authKey = authKey;
            return this;
        }

        public IAzureCosmosProviderConfigurator WithId(Func<AuditEvent, string> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }


#if IS_COSMOS
        public IAzureCosmosProviderConfigurator CosmosClient(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
            return this;
        }
        public IAzureCosmosProviderConfigurator ClientOptions(Action<CosmosClientOptions> cosmosClientOptionsAction)
        {
            _cosmosClientOptionsAction = cosmosClientOptionsAction;
            return this;
        }
#else
        public IAzureCosmosProviderConfigurator ConnectionPolicy(Func<AuditEvent, ConnectionPolicy> connectionPolicyBuilder)
        {
            _connectionPolicy = connectionPolicyBuilder;
            return this;
        }
        public IAzureCosmosProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy)
        {
            _connectionPolicy = connectionPolicy;
            return this;
        }
        public IAzureCosmosProviderConfigurator DocumentClient(IDocumentClient documentClient)
        {
            _documentClient = documentClient;
            return this;
        }
#endif
    }
}