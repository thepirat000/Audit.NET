using System;
using System.IO;
using System.Text;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Audit.AzureDocumentDB.Providers
{
    /// <summary>
    /// Azure Document DB data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: Server url
    /// - AuthKey: Auth key for the Azure API
    /// - Database: Database name
    /// - Collection: Collection name
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

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            Document doc = client.CreateDocumentAsync(collectionUri, auditEvent).Result;
            return doc.Id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var client = GetClient();
            var docUri = UriFactory.CreateDocumentUri(_database, _collection, docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            client.ReplaceDocumentAsync(docUri, doc).Wait();
        }

        private bool TestConnection()
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
