using System;
using Audit.Core;
#if IS_COSMOS
using Microsoft.Azure.Cosmos;
#else
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
#endif

namespace Audit.AzureCosmos.ConfigurationApi
{
    public interface IAzureCosmosProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Cosmos endpoint URL for the given AuditEvent.
        /// </summary>
        /// <param name="endpointBuilder">The endpoint URL builder.</param>
        IAzureCosmosProviderConfigurator Endpoint(Func<AuditEvent, string> endpointBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos endpoint URL.
        /// </summary>
        /// <param name="endpoint">The endpoint URL.</param>
        IAzureCosmosProviderConfigurator Endpoint(string endpoint);
        /// <summary>
        /// Specifies the Azure Cosmos database name for the given AuditEvent.
        /// </summary>
        /// <param name="databaseBuilder">The database name builder.</param>
        IAzureCosmosProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos database name.
        /// </summary>
        /// <param name="database">The database name.</param>
        IAzureCosmosProviderConfigurator Database(string database);
        /// <summary>
        /// Specifies the Azure Cosmos container name for the given AuditEvent.
        /// </summary>
        /// <param name="containerBuilder">The container name builder.</param>
        IAzureCosmosProviderConfigurator Container(Func<AuditEvent, string> containerBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos container name.
        /// </summary>
        /// <param name="container">The container name.</param>
        IAzureCosmosProviderConfigurator Container(string container);
        /// <summary>
        /// Specifies the Azure Cosmos Auth Key fir the given AuditEvent.
        /// </summary>
        /// <param name="authKeyBuilder">The auth key builder.</param>
        IAzureCosmosProviderConfigurator AuthKey(Func<AuditEvent, string> authKeyBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Auth Key.
        /// </summary>
        /// <param name="authKey">The auth key.</param>
        IAzureCosmosProviderConfigurator AuthKey(string authKey);
        /// <summary>
        /// The document id to use for the given audit event. Default is to generate a random Guid as the id.
        /// </summary>
        IAzureCosmosProviderConfigurator WithId(Func<AuditEvent, string> idBuilder);
#if IS_COSMOS
        /// <summary>
        /// Specifies a custom Azure Cosmos Client to use. When using this setting, Endpoint, AuthKey, and ClientOptions will be ignored.
        /// </summary>
        IAzureCosmosProviderConfigurator CosmosClient(CosmosClient cosmosClient);
        /// <summary>
        /// Allows to change the default Azure Cosmos Client Options 
        /// </summary>
        IAzureCosmosProviderConfigurator ClientOptions(Action<CosmosClientOptions> cosmosClientOptionsAction);
#else
        /// <summary>
        /// Specifies the Azure Cosmos Client Connection Policy builder
        /// </summary>
        /// <param name="connectionPolicyBuilder">The connection policy builder.</param>
        IAzureCosmosProviderConfigurator ConnectionPolicy(Func<AuditEvent, ConnectionPolicy> connectionPolicyBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Client.
        /// </summary>
        /// <param name="documentClient">The configured document client object.</param>
        IAzureCosmosProviderConfigurator DocumentClient(IDocumentClient documentClient);
        /// <summary>
        /// Specifies the Azure DocumentDB Client Connection Policy
        /// </summary>
        /// <param name="connectionPolicy">The connection policy.</param>
        IAzureCosmosProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy);
#endif
    }
}