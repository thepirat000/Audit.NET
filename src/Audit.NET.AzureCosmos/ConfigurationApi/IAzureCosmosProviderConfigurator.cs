using System;
using Audit.Core;
using Microsoft.Azure.Cosmos;

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

        /// <summary>
        /// Specifies a custom Azure Cosmos Client to use. When using this setting, Endpoint, AuthKey, and ClientOptions will be ignored.
        /// </summary>
        IAzureCosmosProviderConfigurator CosmosClient(CosmosClient cosmosClient);
        /// <summary>
        /// Allows to change the default Azure Cosmos Client Options 
        /// </summary>
        IAzureCosmosProviderConfigurator ClientOptions(Action<CosmosClientOptions> cosmosClientOptionsAction);
    }
}