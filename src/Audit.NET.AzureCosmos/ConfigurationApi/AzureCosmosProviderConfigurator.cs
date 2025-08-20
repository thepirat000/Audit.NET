using System;
using Audit.Core;
using Microsoft.Azure.Cosmos;

namespace Audit.AzureCosmos.ConfigurationApi
{
    public class AzureCosmosProviderConfigurator : IAzureCosmosProviderConfigurator
    {
        internal Setting<string> _endpoint = string.Empty;
        internal Setting<string> _authKey;
        internal Setting<string> _database = "Audit";
        internal Setting<string> _container = "Events";
        internal Func<AuditEvent, string> _idBuilder;
        internal CosmosClient _cosmosClient;
        internal Action<CosmosClientOptions> _cosmosClientOptionsAction;

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
    }
}