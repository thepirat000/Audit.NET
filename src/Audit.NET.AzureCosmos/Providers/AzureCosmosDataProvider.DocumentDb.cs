#if IS_DOCDB
using System;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Audit.AzureCosmos.ConfigurationApi;
using System.Threading;

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
        public Func<AuditEvent, string> EndpointBuilder { get; set; }
        /// <summary>
        /// Sets the endpoint URL.
        /// </summary>
        public string Endpoint { set { EndpointBuilder = _ => value; } }
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
        /// A func that returns the Container to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> ContainerBuilder { get; set; }
        /// <summary>
        /// Sets the Container to use.
        /// </summary>
        public string Container { set { ContainerBuilder = _ => value; } }
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
        /// A func that returns the document id to use for a given audit event. By default it will generate a new random Guid as the id.
        /// </summary>
        public Func<AuditEvent, string> IdBuilder { get; set; }

        public AzureCosmosDataProvider()
        {
        }

        public AzureCosmosDataProvider(Action<IAzureCosmosProviderConfigurator> config)
        {
            var cosmosDbConfig = new AzureCosmosProviderConfigurator();
            config.Invoke(cosmosDbConfig);
            EndpointBuilder = cosmosDbConfig._endpointBuilder;
            AuthKeyBuilder = cosmosDbConfig._authKeyBuilder;
            ContainerBuilder = cosmosDbConfig._containerBuilder;
            DatabaseBuilder = cosmosDbConfig._databaseBuilder;
            ConnectionPolicyBuilder = cosmosDbConfig._connectionPolicyBuilder;
            DocumentClient = cosmosDbConfig._documentClient;
            IdBuilder = cosmosDbConfig._idBuilder;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            SetId(auditEvent);
            Document doc = client.CreateDocumentAsync(collectionUri, auditEvent, new RequestOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings })
                .GetAwaiter().GetResult();
            return doc.Id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var client = GetClient(auditEvent);
            var collectionUri = GetCollectionUri(auditEvent);
            SetId(auditEvent);
            Document doc = await client.CreateDocumentAsync(collectionUri, auditEvent, new RequestOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings }, cancellationToken: cancellationToken);
            return doc.Id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(auditEvent), ContainerBuilder?.Invoke(auditEvent), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
            }
            doc.Id = docId.ToString();
            client.ReplaceDocumentAsync(docUri, doc, new RequestOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings }).Wait();
        }
        
        public override async Task ReplaceEventAsync(object docId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var client = GetClient(auditEvent);
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(auditEvent), ContainerBuilder?.Invoke(auditEvent), docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
            }
            doc.Id = docId.ToString();
            await client.ReplaceDocumentAsync(docUri, doc, new RequestOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id or a Tuple&lt;string, string&gt; / ValueTuple&lt;string, string&gt; of id and partitionKey. 
        /// </summary>
        public override T GetEvent<T>(object id)
        {
#if !NET45
            if (id is ValueTuple<string, string> vt)
            {
                return GetEvent<T>(vt.Item1, vt.Item2);
            }
#endif
            if (id is Tuple<string, string> t)
            {
                return GetEvent<T>(t.Item1, t.Item2);
            }
            return GetEvent<T>(id?.ToString(), null);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id or a Tuple&lt;string, string&gt; / ValueTuple&lt;string, string&gt; of id and partitionKey. 
        /// </summary>
        public override async Task<T> GetEventAsync<T>(object id, CancellationToken cancellationToken = default)
        {
#if !NET45
            if (id is ValueTuple<string, string> vt)
            {
                return await GetEventAsync<T>(vt.Item1, vt.Item2, cancellationToken);
            }
#endif
            if (id is Tuple<string, string> t)
            {
                return await GetEventAsync<T>(t.Item1, t.Item2, cancellationToken);
            }
            return await GetEventAsync<T>(id?.ToString(), null, cancellationToken);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public T GetEvent<T>(string docId, string partitionKey)
        {
            return GetEventAsync<T>(docId, partitionKey).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public AuditEvent GetEvent(string docId, string partitionKey)
        {
            return GetEvent<AuditEvent>(docId, partitionKey);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public async Task<AuditEvent> GetEventAsync(string docId, string partitionKey)
        {
            return await GetEventAsync<AuditEvent>(docId, partitionKey);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public async Task<T> GetEventAsync<T>(string docId, string partitionKey, CancellationToken cancellationToken = default)
        {
            var client = GetClient(null);
            var docUri = UriFactory.CreateDocumentUri(DatabaseBuilder?.Invoke(null), ContainerBuilder?.Invoke(null), docId);
#if NET45
            var pk = new PartitionKey(partitionKey);
#else
            var pk = partitionKey == null ? PartitionKey.None : new PartitionKey(partitionKey);
#endif
            return (await client.ReadDocumentAsync<T>(docUri, new RequestOptions() { PartitionKey = pk }, cancellationToken)).Document;
        }

        private IDocumentClient GetClient(AuditEvent auditEvent)
        {
            return DocumentClient ?? InitializeClient(auditEvent);
        }

        private IDocumentClient InitializeClient(AuditEvent auditEvent)
        {
            var policy = ConnectionPolicyBuilder?.Invoke()
                ?? new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                };
            DocumentClient = new DocumentClient(new Uri(EndpointBuilder!.Invoke(auditEvent)), AuthKeyBuilder?.Invoke(auditEvent), policy);
            Task.Run(() => { ((DocumentClient)DocumentClient).OpenAsync(); });

            return DocumentClient;
        }

        private Uri GetCollectionUri(AuditEvent auditEvent)
        {
            return UriFactory.CreateDocumentCollectionUri(DatabaseBuilder?.Invoke(auditEvent), ContainerBuilder.Invoke(auditEvent));
        }

        private void SetId(AuditEvent auditEvent)
        {
            if (!auditEvent.CustomFields.ContainsKey("id") && IdBuilder != null)
            {
                var id = IdBuilder.Invoke(auditEvent) ?? Guid.NewGuid().ToString();
                auditEvent.CustomFields["id"] = id;
            }
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<AuditEvent> QueryEvents(FeedOptions feedOptions = null)
        {
            var client = GetClient(null);
            var collectionUri = GetCollectionUri(null);
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, feedOptions ?? new FeedOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings });
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<T> QueryEvents<T>(FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient(null);
            var collectionUri = GetCollectionUri(null);
            return client.CreateDocumentQuery<T>(collectionUri, feedOptions ?? new FeedOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings });
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<AuditEvent> EnumerateEvents(string sqlExpression, FeedOptions feedOptions = null)
        {
            var client = GetClient(null);
            var collectionUri = GetCollectionUri(null);
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, sqlExpression, feedOptions ?? new FeedOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings });
        }
        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<T> EnumerateEvents<T>(string sqlExpression, FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient(null);
            var collectionUri = GetCollectionUri(null);
            return client.CreateDocumentQuery<T>(collectionUri, sqlExpression, feedOptions ?? new FeedOptions() { JsonSerializerSettings = Core.Configuration.JsonSettings });
        }
    }
}
#endif