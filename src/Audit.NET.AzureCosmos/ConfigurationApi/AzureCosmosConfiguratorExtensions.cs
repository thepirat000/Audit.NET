using Audit.AzureCosmos.ConfigurationApi;
using Audit.AzureCosmos.Providers;
using Audit.Core.ConfigurationApi;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;

namespace Audit.Core
{
    public static class AzureCosmosConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Cosmos database.
        /// </summary>
        /// <param name="endpoint">The Azure Cosmos endpoint URL.</param>
        /// <param name="database">The Azure Cosmos database name.</param>
        /// <param name="container">The Azure Cosmos container name.</param>
        public static ICreationPolicyConfigurator UseAzureCosmos(
            this IConfigurator configurator, string endpoint,
            string authKey = null, string database = "Audit", string container = "Event",
            ConnectionPolicy connectionPolicy = null, IDocumentClient documentClient = null)
        {
            Configuration.DataProvider = new AzureCosmosDataProvider()
            {
                Endpoint = endpoint,
                AuthKey = authKey,
                Container = container,
                Database = database,
                ConnectionPolicy = connectionPolicy,
                DocumentClient = documentClient
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in an Azure Cosmos database.
        /// </summary>
        /// <param name="config">The Azure Cosmos provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureCosmos(
            this IConfigurator configurator, Action<IAzureCosmosProviderConfigurator> config)
        {
            var cosmosDbConfig = new AzureCosmosProviderConfigurator();
            config.Invoke(cosmosDbConfig);

            Configuration.DataProvider = new AzureCosmosDataProvider()
            {
                EndpointBuilder = cosmosDbConfig._endpointBuilder,
                AuthKeyBuilder = cosmosDbConfig._authKeyBuilder,
                ContainerBuilder = cosmosDbConfig._containerBuilder,
                DatabaseBuilder = cosmosDbConfig._databaseBuilder,
                ConnectionPolicyBuilder = cosmosDbConfig._connectionPolicyBuilder,
                DocumentClient = cosmosDbConfig._documentClient
            };
            return new CreationPolicyConfigurator();
        }
    }
}
