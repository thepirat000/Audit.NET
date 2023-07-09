using MongoDB.Driver;

namespace Audit.MongoDB.ConfigurationApi
{
    public class MongoProviderConfigurator : IMongoProviderConfigurator
    {
        internal string _connectionString = "mongodb://localhost:27017";
        internal string _database = "Audit";
        internal string _collection = "Event";
        internal bool _serializeAsBson = false;
        internal MongoClientSettings _clientSettings = null;
        internal MongoDatabaseSettings _databaseSettings = null;

        public IMongoProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public IMongoProviderConfigurator ClientSettings(MongoClientSettings mongoClientSettings)
        {
            _clientSettings = mongoClientSettings;
            return this;
        }

        public IMongoProviderConfigurator DatabaseSettings(MongoDatabaseSettings mongoDatabaseSettings)
        {
            _databaseSettings = mongoDatabaseSettings;
            return this;
        }

        public IMongoProviderConfigurator Database(string database)
        {
            _database = database;
            return this;
        }

        public IMongoProviderConfigurator Collection(string collection)
        {
            _collection = collection;
            return this;
        }

        public IMongoProviderConfigurator SerializeAsBson(bool value = true)
        {
            _serializeAsBson = value;
            return this;
        }
    }
}