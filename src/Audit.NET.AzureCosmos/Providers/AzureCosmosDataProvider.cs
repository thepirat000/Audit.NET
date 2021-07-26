using System;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Audit.AzureCosmos.Providers
{
    /// <summary>
    /// Azure Cosmos DB (Document DB SQL API) data provider
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - Endpoint: Server url
    /// - AuthKey: Auth key for the Azure API
    /// - Database: Database name
    /// - Container: Container name
    /// - ConnectionPolicy: The client connection policy settings
    /// - DocumentClient: A document client that is reused by your app
    /// </remarks>
    public class AzureCosmosDataProvider : AuditDataProvider
    {
        /// <summary>
        /// A func that returns the endpoint URL to use.
        /// </summary>
        public Func<string> EndpointBuilder { get; set; }
        /// <summary>
        /// Sets the endpoint URL.
        /// </summary>
        public string Endpoint { set { EndpointBuilder = () => value; } }
        /// <summary>
        /// A func that returns the AuthKey to use for a given audit event.
        /// </summary>
        public Func<string> AuthKeyBuilder { get; set; }
        /// <summary>
        /// Sets the AuthKey to use.
        /// </summary>
        public string AuthKey { set { AuthKeyBuilder = () => value; } }
        /// <summary>
        /// A func that returns the Database to use for a given audit event.
        /// </summary>
        public Func<string> DatabaseBuilder { get; set; }
        /// <summary>
        /// Sets the Database to use.
        /// </summary>
        public string Database { set { DatabaseBuilder = () => value; } }
        /// <summary>
        /// A func that returns the Container to use for a given audit event.
        /// </summary>
        public Func<string> ContainerBuilder { get; set; }
        /// <summary>
        /// Sets the Container to use.
        /// </summary>
        public string Container { set { ContainerBuilder = () => value; } }
        /// <summary>
        /// A func that returns the ConnectionPolicy to use for a given audit event.
        /// </summary>
        public Func<ConnectionPolicy> ConnectionPolicyBuilder { get; set; }
        /// <summary>
        /// Sets the ConnectionPolicy to use.
        /// </summary>
        public ConnectionPolicy ConnectionPolicy { set { ConnectionPolicyBuilder = () => value; } }
        /// <summary>
        /// Gets or Sets the custom DocumentClient to use. Default is NULL to use an internal cached client.
        /// </summary>
        public IDocumentClient DocumentClient { get; set; }

        /// <summary>
        /// Gets or sets the JSON serializer settings.
        /// </summary>
        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public override object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string)
            {
                return value;
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, JsonSettings), JsonSettings);
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            Document doc = client.CreateDocumentAsync(collectionUri, auditEvent, new RequestOptions() { JsonSerializerSettings = JsonSettings }).Result;
            return doc.Id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            Document doc = await client.CreateDocumentAsync(collectionUri, auditEvent, new RequestOptions() { JsonSerializerSettings = JsonSettings });
            return doc.Id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var client = GetClient();
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(), ContainerBuilder?.Invoke(), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(auditEvent, JsonSettings))))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            client.ReplaceDocumentAsync(docUri, doc, new RequestOptions() { JsonSerializerSettings = JsonSettings }).Wait();
        }

        public override async Task ReplaceEventAsync(object docId, AuditEvent auditEvent)
        {
            var client = GetClient();
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(), ContainerBuilder?.Invoke(), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(auditEvent, JsonSettings))))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            await client.ReplaceDocumentAsync(docUri, doc, new RequestOptions() { JsonSerializerSettings = JsonSettings });
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="docId">The document id</param>
        public override T GetEvent<T>(object docId)
        {
            return GetEvent<T>(docId?.ToString());
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id.
        /// </summary>
        /// <param name="docId">The document id</param>
        public T GetEvent<T>(string docId)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            var sql = new SqlQuerySpec($"SELECT * FROM {ContainerBuilder?.Invoke()} c WHERE c.id = @id",
                new SqlParameterCollection(new SqlParameter[] { new SqlParameter() { Name = "@id", Value = docId.ToString() } }));
            return client.CreateDocumentQuery(collectionUri, sql, new FeedOptions() { EnableCrossPartitionQuery = true, JsonSerializerSettings = JsonSettings })
                .AsEnumerable()
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id on the collection returned by calling the collection builder with a null AuditEvent.
        /// </summary>
        /// <param name="eventId">The event id</param>
        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            return await GetEventAsync<T>(eventId?.ToString());
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id.
        /// </summary>
        /// <param name="docId">The document id</param>
        public async Task<T> GetEventAsync<T>(string docId)
        {
            return await Task.FromResult(GetEvent<T>(docId));
        }
        
        private IDocumentClient GetClient()
        {
            return DocumentClient ?? InitializeClient();
        }

        private IDocumentClient InitializeClient()
        {
            var policy = ConnectionPolicyBuilder?.Invoke()
                ?? new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                };
            DocumentClient = new DocumentClient(new Uri(EndpointBuilder?.Invoke()), AuthKeyBuilder?.Invoke(), policy);
            Task.Run(() => { ((DocumentClient)DocumentClient).OpenAsync(); });

            return DocumentClient;
        }

        private Uri GetCollectionUri()
        {
            return UriFactory.CreateDocumentCollectionUri(DatabaseBuilder?.Invoke(), ContainerBuilder.Invoke());
        }

#region Events Query        
        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<AuditEvent> QueryEvents(FeedOptions feedOptions = null)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, feedOptions ?? new FeedOptions() { JsonSerializerSettings = JsonSettings });
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<T> QueryEvents<T>(FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<T>(collectionUri, feedOptions ?? new FeedOptions() { JsonSerializerSettings = JsonSettings });
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<AuditEvent> EnumerateEvents(string sqlExpression, FeedOptions feedOptions = null)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, sqlExpression, feedOptions ?? new FeedOptions() { JsonSerializerSettings = JsonSettings });
        }
        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<T> EnumerateEvents<T>(string sqlExpression, FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<T>(collectionUri, sqlExpression, feedOptions ?? new FeedOptions() { JsonSerializerSettings = JsonSettings });
        }
#endregion
    }
}
