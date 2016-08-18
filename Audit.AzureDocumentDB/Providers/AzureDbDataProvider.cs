using System;
using Audit.Core;
using Microsoft.Azure.Documents.Client;

namespace Audit.AzureDocumentDB.Providers
{
    /// <summary>
    /// Azure Document DB data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - AuditConnectionString: Server url
    /// - AuditAuthKey: Auth key for the Azure API
    /// - AuditEventDatabase: Database name
    /// - AuditEventTable: Collection name
    /// </remarks>
    public class AzureDbDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _authKey;
        private string _database;
        private string _collection;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public string AuthKey
        {
            get { return _authKey; }
            set { _authKey = value; }
        }

        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }

        public string Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        public override void WriteEvent(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            client.CreateDocumentAsync(collectionUri, auditEvent).Wait();
        }

        public override bool TestConnection()
        {
            try
            {
                var client = GetClient();
                client.OpenAsync().Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DocumentClient GetClient()
        {
            return new DocumentClient(new Uri(_connectionString), _authKey);
        }

        private Uri GetCollectionUri()
        {
            return UriFactory.CreateDocumentCollectionUri(_database, _collection);
        }
    }
}
