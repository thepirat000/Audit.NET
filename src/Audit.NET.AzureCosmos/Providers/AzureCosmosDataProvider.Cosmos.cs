#if IS_COSMOS
using System;
using Audit.Core;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Audit.AzureCosmos.ConfigurationApi;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

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
    /// - IdBuilder: The document id to use for a given audit event. By default it will generate a new random Guid as the id.
    /// - CosmosClient: A custom cosmos client to use. 
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
        /// A func that returns the AuthKey to use
        /// </summary>
        public Func<AuditEvent, string> AuthKeyBuilder { get; set; }
        /// <summary>
        /// Sets the AuthKey to use.
        /// </summary>
        public string AuthKey { set { AuthKeyBuilder = _ => value; } }
        /// <summary>
        /// A func that returns the Database name to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> DatabaseBuilder { get; set; }
        /// <summary>
        /// Sets the Database name to use.
        /// </summary>
        public string Database { set { DatabaseBuilder = _ => value; } }
        /// <summary>
        /// A function that returns the Container name to use for a given audit event.
        /// </summary>
        public Func<AuditEvent, string> ContainerBuilder { get; set; }
        /// <summary>
        /// Sets the Container name to use.
        /// </summary>
        public string Container { set { ContainerBuilder = _ => value; } }
        /// <summary>
        /// Allows to change the CosmosClientOptions when using the default cosmos client
        /// </summary>
        public Action<CosmosClientOptions> CosmosClientOptionsAction { get; set; }
        /// <summary>
        /// Gets or Sets the custom CosmosClient to use. Default is NULL to use an internal cached client.
        /// </summary>
        public CosmosClient CosmosClient { get; set; }
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
            CosmosClient = cosmosDbConfig._cosmosClient;
            CosmosClientOptionsAction = cosmosDbConfig._cosmosClientOptionsAction;
            IdBuilder = cosmosDbConfig._idBuilder;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var container = GetContainer(auditEvent);
            var id = GetSetId(auditEvent);
            container.CreateItemAsync(auditEvent).GetAwaiter().GetResult();
            return id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var container = GetContainer(auditEvent);
            var id = GetSetId(auditEvent);
            await container.CreateItemAsync(auditEvent, cancellationToken: cancellationToken);
            return id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var container = GetContainer(auditEvent);
            var id = docId.ToString();
            container.ReplaceItemAsync(auditEvent, id).GetAwaiter().GetResult();
        }

        public override async Task ReplaceEventAsync(object docId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var container = GetContainer(auditEvent);
            var id = docId.ToString();
            await container.ReplaceItemAsync(auditEvent, id, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its document id or a Tuple&lt;string, string&gt; / ValueTuple&lt;string, string&gt; of id and partitionKey. 
        /// </summary>
        public override T GetEvent<T>(object id)
        {
            if (id is ValueTuple<string, string> vt)
            {
                return GetEvent<T>(vt.Item1, vt.Item2);
            }
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
            if (id is ValueTuple<string, string> vt)
            {
                return await GetEventAsync<T>(vt.Item1, vt.Item2, cancellationToken);
            }
            if (id is Tuple<string, string> t)
            {
                return await GetEventAsync<T>(t.Item1, t.Item2, cancellationToken);
            }
            return await GetEventAsync<T>(id?.ToString(), null, cancellationToken);
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public T GetEvent<T>(string id, string partitionKey)
        {
            var container = GetContainer(null);
            return container.ReadItemAsync<T>(id, partitionKey == null ? PartitionKey.None : new PartitionKey(partitionKey)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets an event stored on cosmos DB from its id and partition key.
        /// </summary>
        public async Task<T> GetEventAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default)
        {
            var container = GetContainer(null);
            return await container.ReadItemAsync<T>(id, partitionKey == null ? PartitionKey.None : new PartitionKey(partitionKey), cancellationToken: cancellationToken);
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
        public async Task<AuditEvent> GetEventAsync(string docId, string partitionKey, CancellationToken cancellationToken = default)
        {
            return await GetEventAsync<AuditEvent>(docId, partitionKey, cancellationToken);
        }

        private CosmosClient GetClient(AuditEvent auditEvent)
        {
            return CosmosClient ?? InitializeClient(auditEvent);
        }

        private CosmosClient InitializeClient(AuditEvent auditEvent)
        {
            var options = new CosmosClientOptions
            {
                Serializer = new AuditCosmosSerializer()
            };
            if (CosmosClientOptionsAction != null)
            {
                CosmosClientOptionsAction.Invoke(options);
            }
            CosmosClient = new CosmosClient(EndpointBuilder?.Invoke(auditEvent), AuthKeyBuilder?.Invoke(auditEvent), options);
            return CosmosClient;
        }

        private Container GetContainer(AuditEvent auditEvent)
        {
            var client = GetClient(auditEvent);
            return client.GetContainer(DatabaseBuilder?.Invoke(auditEvent), ContainerBuilder?.Invoke(auditEvent));
        }

        private string GetSetId(AuditEvent auditEvent)
        {
            string id;
            if (IdBuilder != null)
            {
                id = IdBuilder?.Invoke(auditEvent);
                auditEvent.CustomFields["id"] = id;
            }
            else
            {
                if (!auditEvent.CustomFields.ContainsKey("id"))
                {
                    id = Guid.NewGuid().ToString().Replace("-", "");
                    auditEvent.CustomFields["id"] = id;
                }
                else
                {
                    id = auditEvent.CustomFields["id"]?.ToString();
                }
            }
            return id;
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <param name="options">The options for processing the query.</param>
        public IQueryable<AuditEvent> QueryEvents(QueryRequestOptions options = null)
        {
            var container = GetContainer(null);
            return container.GetItemLinqQueryable<AuditEvent>(true, null, options);
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Cosmos.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="options">The options for processing the query.</param>
        public IQueryable<T> QueryEvents<T>(QueryRequestOptions options = null) where T : AuditEvent
        {
            var container = GetContainer(null);
            return container.GetItemLinqQueryable<T>(true, null, options);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="queryOptions">The options for processing the query results feed.</param>
        public IAsyncEnumerable<AuditEvent> EnumerateEvents(string sqlExpression, QueryRequestOptions queryOptions = null)
        {
            return EnumerateEvents<AuditEvent>(sqlExpression, queryOptions);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Cosmos SQL expression.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="sqlExpression">The Azure Cosmos SQL expression</param>
        /// <param name="queryOptions">The options for processing the query results feed.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public async IAsyncEnumerable<T> EnumerateEvents<T>(string sqlExpression, QueryRequestOptions queryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var container = GetContainer(null);
            var feed = container.GetItemQueryIterator<T>(sqlExpression, null, queryOptions);
            while (feed.HasMoreResults)
            {
                foreach (var auditEvent in await feed.ReadNextAsync(cancellationToken))
                {
                    yield return auditEvent;
                }
            }
        }

    }
}
#endif