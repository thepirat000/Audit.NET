using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;

namespace Audit.AzureCosmos.ConfigurationApi
{
    public interface IAzureCosmosProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Cosmos endpoint URL.
        /// </summary>
        /// <param name="endpoint">The endpoint URL.</param>
        IAzureCosmosProviderConfigurator Endpoint(string endpoint);
        /// <summary>
        /// Specifies the Azure Cosmos database name.
        /// </summary>
        /// <param name="database">The database name.</param>
        IAzureCosmosProviderConfigurator Database(string database);
        /// <summary>
        /// Specifies the Azure Cosmos container name.
        /// </summary>
        /// <param name="container">The container name.</param>
        IAzureCosmosProviderConfigurator Container(string container);
        /// <summary>
        /// Specifies the Azure Cosmos Auth Key.
        /// </summary>
        /// <param name="authKey">The auth key.</param>
        IAzureCosmosProviderConfigurator AuthKey(string authKey);
        /// <summary>
        /// Specifies the Azure DocumentDB Client Connection Policy
        /// </summary>
        /// <param name="connectionPolicy">The connection policy.</param>
        IAzureCosmosProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy);
        /// <summary>
        /// Specifies the Azure Cosmos endpoint URL builder.
        /// </summary>
        /// <param name="endpointBuilder">The endpoint builder.</param>
        IAzureCosmosProviderConfigurator Endpoint(Func<string> endpointBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos database name builder.
        /// </summary>
        /// <param name="databaseBuilder">The database name builder.</param>
        IAzureCosmosProviderConfigurator Database(Func<string> databaseBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Container name builder.
        /// </summary>
        /// <param name="containerBuilder">The Container name builder.</param>
        IAzureCosmosProviderConfigurator Container(Func<string> containerBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Auth Key builder.
        /// </summary>
        /// <param name="authKeyBuilder">The auth key builder.</param>
        IAzureCosmosProviderConfigurator AuthKey(Func<string> authKeyBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Client Connection Policy builder
        /// </summary>
        /// <param name="connectionPolicyBuilder">The connection policy builder.</param>
        IAzureCosmosProviderConfigurator ConnectionPolicy(Func<ConnectionPolicy> connectionPolicyBuilder);
        /// <summary>
        /// Specifies the Azure Cosmos Client.
        /// </summary>
        /// <param name="documentClient">The configured document client object.</param>
        IAzureCosmosProviderConfigurator DocumentClient(IDocumentClient documentClient);
    }
}