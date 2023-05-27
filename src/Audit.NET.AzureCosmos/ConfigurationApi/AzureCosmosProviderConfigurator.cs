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
        internal Func<AuditEvent, string> _endpointBuilder = _ => string.Empty;
        internal Func<AuditEvent, string> _authKeyBuilder = _ => null;
        internal Func<AuditEvent, string> _databaseBuilder = _ => "Audit";
        internal Func<AuditEvent, string> _containerBuilder = _ => "Events";
        internal Func<AuditEvent, string> _idBuilder;
#if IS_COSMOS
        internal CosmosClient _cosmosClient;
        internal Action<CosmosClientOptions> _cosmosClientOptionsAction;
#else
        internal Func<ConnectionPolicy> _connectionPolicyBuilder = () => null;
        internal IDocumentClient _documentClient = null;
#endif

        public IAzureCosmosProviderConfigurator Endpoint(Func<AuditEvent, string> endpointBuilder)
        {
            _endpointBuilder = endpointBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Endpoint(string endpoint)
        {
            _endpointBuilder = _ => endpoint;
            return this;
        }

        public IAzureCosmosProviderConfigurator Endpoint(Func<string> endpointBuilder)
        {
            _endpointBuilder = _ => endpointBuilder.Invoke();
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(Func<string> databaseBuilder)
        {
            _databaseBuilder = _ => databaseBuilder.Invoke();
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder)
        {
            _databaseBuilder = databaseBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(string database)
        {
            _databaseBuilder = _ => database;
            return this;
        }

        public IAzureCosmosProviderConfigurator Container(Func<string> containerBuilder)
        {
            _containerBuilder = _ => containerBuilder.Invoke();
            return this;
        }

        public IAzureCosmosProviderConfigurator Container(Func<AuditEvent, string> containerBuilder)
        {
            _containerBuilder = containerBuilder;
            return this;
        }
        
        public IAzureCosmosProviderConfigurator Container(string container)
        {
            _containerBuilder = _ => container;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(Func<string> authKeyBuilder)
        {
            _authKeyBuilder = _ => authKeyBuilder.Invoke();
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(Func<AuditEvent, string> authKeyBuilder)
        {
            _authKeyBuilder = authKeyBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(string authKey)
        {
            _authKeyBuilder = _ => authKey;
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
        public IAzureCosmosProviderConfigurator ConnectionPolicy(Func<ConnectionPolicy> connectionPolicyBuilder)
        {
            _connectionPolicyBuilder = connectionPolicyBuilder;
            return this;
        }
        public IAzureCosmosProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy)
        {
            _connectionPolicyBuilder = () => connectionPolicy;
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