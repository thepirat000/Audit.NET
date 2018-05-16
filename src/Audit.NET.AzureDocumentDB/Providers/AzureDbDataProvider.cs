using System;
using System.IO;
using System.Text;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    /// - ConnectionPolicy: The client connection policy settings
    /// - DocumentClient: A document client that is reused by your app
    /// </remarks>
    public class AzureDbDataProvider : AuditDataProvider
    {
        /// <summary>
        /// A func that returns the connection string to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder { get; set; }
        /// <summary>
        /// Sets the connection string.
        /// </summary>
        public string ConnectionString { set { ConnectionStringBuilder = _ => value; } }
        /// <summary>
        /// A func that returns the AuthKey to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> AuthKeyBuilder { get; set; }
        /// <summary>
        /// Sets the AuthKey to use.
        /// </summary>
        public string AuthKey { set { AuthKeyBuilder = _ => value; } }

        /// <summary>
        /// A func that returns the Database to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> DatabaseBuilder { get; set; }
        /// <summary>
        /// Sets the Database to use.
        /// </summary>
        public string Database { set { DatabaseBuilder = _ => value; } }
        /// <summary>
        /// A func that returns the Collection to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> CollectionBuilder { get; set; }
        /// <summary>
        /// Sets the Collection to use.
        /// </summary>
        public string Collection { set { CollectionBuilder = _ => value; } }
        /// <summary>
        /// A func that returns the ConnectionPolicy to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, ConnectionPolicy> ConnectionPolicyBuilder { get; set; }
        /// <summary>
        /// Sets the ConnectionPolicy to use.
        /// </summary>
        public ConnectionPolicy ConnectionPolicy { set { ConnectionPolicyBuilder = _ => value; } }
        /// <summary>
        /// Gets or Sets the custom DocumentClient to use. Default is NULL to use an internal cached client.
        /// </summary>
        public IDocumentClient DocumentClient { get; set; }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            Document doc = client.CreateDocumentAsync(collectionUri, auditEvent).Result;
            return doc.Id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            Document doc = await client.CreateDocumentAsync(collectionUri, auditEvent);
            return doc.Id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(auditEvent), CollectionBuilder?.Invoke(auditEvent), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            client.ReplaceDocumentAsync(docUri, doc).Wait();
        }

        public override async Task ReplaceEventAsync(object docId, AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(auditEvent), CollectionBuilder?.Invoke(auditEvent), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            await client.ReplaceDocumentAsync(docUri, doc);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="docId">The document id</param>
        public override T GetEvent<T>(object docId)
        {
            return GetEvent<T>(docId?.ToString(), null);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="docId">The document id</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public T GetEvent<T>(string docId, AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            var sql = new SqlQuerySpec($"SELECT * FROM {CollectionBuilder?.Invoke(auditEvent)} c WHERE c.id = @id",
                new SqlParameterCollection(new SqlParameter[] { new SqlParameter() { Name = "@id", Value = docId.ToString() } }));
            return client.CreateDocumentQuery(collectionUri, sql)
                .AsEnumerable()
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="docId">The document id</param>
        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            return await Task.FromResult(GetEvent<T>(eventId));
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="docId">The document id</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public async Task<T> GetEventAsync<T>(string docId, AuditEvent auditEvent)
        {
            return await Task.FromResult(GetEvent<T>(docId, auditEvent));
        }
        
        private IDocumentClient GetClient(AuditEvent auditEvent)
        {
            return DocumentClient ?? InitializeClient(auditEvent);
        }

        private IDocumentClient InitializeClient(AuditEvent auditEvent)
        {
            var policy = ConnectionPolicyBuilder?.Invoke(auditEvent)
                ?? new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                };

            DocumentClient = new DocumentClient(new Uri(ConnectionStringBuilder?.Invoke(auditEvent)), AuthKeyBuilder?.Invoke(auditEvent), Configuration.JsonSettings, policy);
            Task.Run(() => { ((DocumentClient)DocumentClient).OpenAsync(); });

            return DocumentClient;
        }

        private Uri GetCollectionUri(AuditEvent auditEvent)
        {
            return UriFactory.CreateDocumentCollectionUri(DatabaseBuilder?.Invoke(auditEvent), CollectionBuilder.Invoke(auditEvent));
        }

#region Events Query        
        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Document DB.
        /// </summary>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public IQueryable<AuditEvent> QueryEvents(FeedOptions feedOptions = null, AuditEvent auditEvent = null)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, feedOptions);
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Document DB, for the audit event type given.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public IQueryable<T> QueryEvents<T>(FeedOptions feedOptions = null, AuditEvent auditEvent = null) where T : AuditEvent
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            return client.CreateDocumentQuery<T>(collectionUri, feedOptions);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Document DB SQL expression.
        /// </summary>
        /// <param name="sqlExpression">The Azure Document DB SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public IEnumerable<AuditEvent> EnumerateEvents(string sqlExpression, FeedOptions feedOptions = null, AuditEvent auditEvent = null)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, sqlExpression, feedOptions);
        }
        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Document DB SQL expression and the event type given.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="sqlExpression">The Azure Document DB SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        /// <param name="auditEvent">The AuditEvent to use when calling the builders to get the ConnectionString, Database, Collection and AuthKey.</param>
        public IEnumerable<T> EnumerateEvents<T>(string sqlExpression, FeedOptions feedOptions = null, AuditEvent auditEvent = null) where T : AuditEvent
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            return client.CreateDocumentQuery<T>(collectionUri, sqlExpression, feedOptions);
        }
#endregion
    }
}
