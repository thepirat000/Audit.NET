using MongoDB.Driver;

namespace Audit.MongoDB.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the Mongo DB data provider
    /// </summary>
    public interface IMongoProviderConfigurator
    {
        /// <summary>
        /// Specifies the Mongo DB connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        IMongoProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the Mongo DB client settings to use.
        /// This setting takes precedence over the ConnectionString.
        /// </summary>
        /// <param name="mongoClientSettings">The Mongo client settings.</param>
        IMongoProviderConfigurator ClientSettings(MongoClientSettings mongoClientSettings);
        /// <summary>
        /// Specifies the Mongo DB database settings to use (optional).
        /// </summary>
        /// <param name="mongoDatabaseSettings">The Mongo database settings.</param>
        IMongoProviderConfigurator DatabaseSettings(MongoDatabaseSettings mongoDatabaseSettings);
        /// <summary>
        /// Specifies the Mongo DB database name.
        /// </summary>
        /// <param name="database">The database name.</param>
        IMongoProviderConfigurator Database(string database);
        /// <summary>
        /// Specifies the Mongo DB collection name.
        /// </summary>
        /// <param name="collection">The collection name.</param>
        IMongoProviderConfigurator Collection(string collection);
        /// <summary>
        /// Specifies whether the target object and extra fields should be serialized as Bson. Default is Json.
        /// </summary>
        /// <param name="value">The setting value.</param>
        IMongoProviderConfigurator SerializeAsBson(bool value = true);
    }
}