using Audit.AzureDocumentDB.ConfigurationApi;
using Audit.AzureDocumentDB.Providers;
using Audit.Core.ConfigurationApi;
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
        public static ICreationPolicyConfigurator UseAzureDocumentDB(this IConfigurator configurator, string connectionString,
            string authKey = null, string database = "Audit", string collection = "Event")
        {
            Configuration.DataProvider = new AzureDbDataProvider()
            {
                ConnectionString = connectionString,
                AuthKey = authKey,
                Collection = collection,
                Database = database
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in an Azure Document DB database.
        /// </summary>
        /// <param name="config">The Document DB provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureDocumentDB(this IConfigurator configurator, Action<IDocumentDbProviderConfigurator> config)
        {
            var documentDbConfig = new DocumentDbProviderConfigurator();
            config.Invoke(documentDbConfig);
            return UseAzureDocumentDB(configurator, documentDbConfig._connectionString, documentDbConfig._authKey, documentDbConfig._database, documentDbConfig._collection);
        }
    }
}
