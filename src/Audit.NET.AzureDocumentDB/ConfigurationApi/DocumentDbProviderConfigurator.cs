namespace Audit.AzureDocumentDB.ConfigurationApi
{
    public class DocumentDbProviderConfigurator : IDocumentDbProviderConfigurator
    {
        internal string _connectionString = "mongodb://localhost:27017";
        internal string _authKey = null;
        internal string _database = "Audit";
        internal string _collection = "Event";
        public IDocumentDbProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public IDocumentDbProviderConfigurator Database(string database)
        {
            _database = database;
            return this;
        }

        public IDocumentDbProviderConfigurator Collection(string collection)
        {
            _collection = collection;
            return this;
        }

        public IDocumentDbProviderConfigurator AuthKey(string authKey)
        {
            _authKey = authKey;
            return this;
        }
    }
}