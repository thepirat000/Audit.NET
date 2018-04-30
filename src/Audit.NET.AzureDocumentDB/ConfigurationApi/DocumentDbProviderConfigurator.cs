using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Audit.AzureDocumentDB.ConfigurationApi
{
    public class DocumentDbProviderConfigurator : IDocumentDbProviderConfigurator
    {
        internal string _connectionString = string.Empty;
        internal string _authKey = null;
        internal string _database = "Audit";
        internal string _collection = "Events";
        internal ConnectionPolicy _connectionPolicy = null;
        internal IDocumentClient _documentClient = null;

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

        public IDocumentDbProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy)
        {
            _connectionPolicy = connectionPolicy;
            return this;
        }
        public IDocumentDbProviderConfigurator DocumentClient(IDocumentClient documentClient)
        {
            _documentClient = documentClient;
            return this;
        }
    }
}