using Audit.AzureDocumentDB.ConfigurationApi;
using Audit.AzureDocumentDB.Providers;
using Audit.Core.ConfigurationApi;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;

namespace Audit.Core
{
    public static class DocumentDbConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Document DB database.
        /// </summary>
        /// <param name="connectionString">The mongo DB connection string.</param>
        /// <param name="database">The mongo DB database name.</param>
        /// <param name="collection">The mongo DB collection name.</param>
        public static ICreationPolicyConfigurator UseAzureDocumentDB(
            this IConfigurator configurator, string connectionString,
            string authKey = null, string database = "Audit", string collection = "Event",
            ConnectionPolicy connectionPolicy = null, IDocumentClient documentClient = null)
        {
            Configuration.DataProvider = new AzureDbDataProvider()
            {
                ConnectionString = connectionString,
                AuthKey = authKey,
                Collection = collection,
                Database = database,
                ConnectionPolicy = connectionPolicy,
                DocumentClient = documentClient
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in an Azure Document DB database.
        /// </summary>
        /// <param name="config">The Document DB provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureDocumentDB(
            this IConfigurator configurator, Action<IDocumentDbProviderConfigurator> config)
        {
            var documentDbConfig = new DocumentDbProviderConfigurator();
            config.Invoke(documentDbConfig);

            Configuration.DataProvider = new AzureDbDataProvider()
            {
                ConnectionStringBuilder = documentDbConfig._connectionStringBuilder,
                AuthKeyBuilder = documentDbConfig._authKeyBuilder,
                CollectionBuilder = documentDbConfig._collectionBuilder,
                DatabaseBuilder = documentDbConfig._databaseBuilder,
                ConnectionPolicyBuilder = documentDbConfig._connectionPolicyBuilder,
                DocumentClient = documentDbConfig._documentClient
            };
            return new CreationPolicyConfigurator();
        }
    }
}
