using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Audit.AzureCosmos.ConfigurationApi
{
    public class AzureCosmosProviderConfigurator : IAzureCosmosProviderConfigurator
    {
        internal Func<string> _endpointBuilder = () => string.Empty;
        internal Func<string> _authKeyBuilder = () => null;
        internal Func<string> _databaseBuilder = () => "Audit";
        internal Func<string> _containerBuilder = () => "Events";
        internal Func<ConnectionPolicy> _connectionPolicyBuilder = () => null;
        internal IDocumentClient _documentClient = null;
        internal JsonSerializerSettings _jsonSettings = null;

        public IAzureCosmosProviderConfigurator Endpoint(string endpoint)
        {
            _endpointBuilder = () => endpoint;
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(string database)
        {
            _databaseBuilder = () => database;
            return this;
        }

        public IAzureCosmosProviderConfigurator Container(string container)
        {
            _containerBuilder = () => container;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(string authKey)
        {
            _authKeyBuilder = () => authKey;
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

        public IAzureCosmosProviderConfigurator Endpoint(Func<string> endpointBuilder)
        {
            _endpointBuilder = endpointBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Database(Func<string> databaseBuilder)
        {
            _databaseBuilder = databaseBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator Container(Func<string> containerBuilder)
        {
            _containerBuilder = containerBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator AuthKey(Func<string> authKeyBuilder)
        {
            _authKeyBuilder = authKeyBuilder;
            return this;
        }

        public IAzureCosmosProviderConfigurator ConnectionPolicy(Func<ConnectionPolicy> connectionPolicyBuilder)
        {
            _connectionPolicyBuilder = connectionPolicyBuilder;
            return this;
        }
        
        public IAzureCosmosProviderConfigurator JsonSettings(JsonSerializerSettings settings)
        {
            _jsonSettings = settings;
            return this;
        }
    }
}