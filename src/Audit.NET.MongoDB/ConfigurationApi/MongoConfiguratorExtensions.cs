using System;
using Audit.MongoDB.Providers;
using Audit.Core.ConfigurationApi;
using Audit.MongoDB.ConfigurationApi;
using Newtonsoft.Json;

namespace Audit.Core
{
    public static class MongoConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a MongoDB database.
        /// </summary>
        /// <param name="connectionString">The mongo DB connection string.</param>
        /// <param name="database">The mongo DB database name.</param>
        /// <param name="collection">The mongo DB collection name.</param>
        /// <param name="jsonSerializerSettings">The custom JsonSerializerSettings.</param>
        public static ICreationPolicyConfigurator UseMongoDB(this IConfigurator configurator, string connectionString = "mongodb://localhost:27017",
            string database = "Audit", string collection = "Event", JsonSerializerSettings jsonSerializerSettings = null)
        {
            Configuration.DataProvider = new MongoDataProvider()
            {
                ConnectionString = connectionString,
                Collection = collection,
                Database = database,
                JsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in a MongoDB database.
        /// </summary>
        /// <param name="config">The mongo DB provider configuration.</param>
        public static ICreationPolicyConfigurator UseMongoDB(this IConfigurator configurator, Action<IMongoProviderConfigurator> config)
        {
            var mongoConfig = new MongoProviderConfigurator();
            config.Invoke(mongoConfig);
            return UseMongoDB(configurator, mongoConfig._connectionString, mongoConfig._database, mongoConfig._collection, mongoConfig._jsonSerializerSettings);
        }
    }
}
