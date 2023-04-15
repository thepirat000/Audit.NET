using System;
using Audit.Core;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Audit.DynamoDB.Providers
{
    /// <summary>
    /// Amazon DynamoDB data provider for Audit.NET. Store the audit events into DynamoDB tables.
    /// </summary>
    public class DynamoDataProvider : AuditDataProvider
    {
        private static readonly ConcurrentDictionary<string, Table> TableCache = new ConcurrentDictionary<string, Table>();

        /// <summary>
        /// Top-level attributes to be added to the event and document before saving
        /// </summary>
        public Dictionary<string, Func<AuditEvent, object>> CustomAttributes { get; set; } = new Dictionary<string, Func<AuditEvent, object>>();

        /// <summary>
        /// Factory that creates the client
        /// </summary>
        public Lazy<IAmazonDynamoDB> Client { get; set; }

        /// <summary>
        /// The DynamoDB table name to use when saving an audit event. 
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; }

        /// <summary>
        /// Creates a new DynamoDB data provider using the given client.
        /// </summary>
        /// <param name="client">The amazon DynamoDB client instance</param>
        public DynamoDataProvider(IAmazonDynamoDB client)
        {
            Client = new Lazy<IAmazonDynamoDB>(() => client);
        }

        /// <summary>
        /// Creates a new DynamoDB data provider using the given client.
        /// </summary>
        /// <param name="client">The amazon DynamoDB client instance</param>
        public DynamoDataProvider(AmazonDynamoDBClient client)
        {
            Client = new Lazy<IAmazonDynamoDB>(() => client);
        }

        /// <summary>
        /// Creates a new DynamoDB data provider.
        /// </summary>
        public DynamoDataProvider()
        {
        }

        /// <summary>
        /// Creates a new DynamoDB data provider with the given configuration options.
        /// </summary>
        public DynamoDataProvider(Action<Configuration.IDynamoProviderConfigurator> config)
        {
            var dynaDbConfig = new Configuration.DynamoProviderConfigurator();
            if (config != null)
            {
                config.Invoke(dynaDbConfig);
                Client = dynaDbConfig._clientFactory;
                TableNameBuilder = dynaDbConfig._tableConfigurator?._tableNameBuilder;
                CustomAttributes = dynaDbConfig._tableConfigurator?._attrConfigurator?._attributes;
            }
        }

        /// <summary>
        /// Inserts an event into DynamoDB
        /// </summary>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var table = GetTable(auditEvent);
            var document = CreateDocument(auditEvent, true);
            table.PutItemAsync(document).GetAwaiter().GetResult();
            return GetKeyValues(document, table);
        }

        /// <summary>
        /// Asynchronously inserts an event into DynamoDB
        /// </summary>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var table = GetTable(auditEvent);
            var document = CreateDocument(auditEvent, true);
            await table.PutItemAsync(document, cancellationToken);
            return GetKeyValues(document, table);
        }

        /// <summary>
        /// Replaces an event into DynamoDB
        /// </summary>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var table = GetTable(auditEvent);
            var document = CreateDocument(auditEvent, false);
            table.PutItemAsync(document).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously replaces an event into DynamoDB
        /// </summary>
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var table = GetTable(auditEvent);
            var document = CreateDocument(auditEvent, false);
            await table.PutItemAsync(document, cancellationToken);
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="hashKey">The primary key Hash portion</param>
        /// <param name="rangeKey">The primary key Range portion, if any. Otherwise NULL.</param>
        public T GetEvent<T>(Primitive hashKey, Primitive rangeKey) where T : AuditEvent
        {
            var table = GetTable(null);
            Document doc;
            if (rangeKey == null)
            {
                doc = table.GetItemAsync(hashKey).GetAwaiter().GetResult();
            }
            else
            {
                doc = table.GetItemAsync(hashKey, rangeKey).GetAwaiter().GetResult();
            }
            return AuditEvent.FromJson<T>(doc.ToJson());
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="hashKey">The primary key Hash portion</param>
        /// <param name="rangeKey">The primary key Range portion, if any. Otherwise NULL.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public async Task<T> GetEventAsync<T>(Primitive hashKey, Primitive rangeKey, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var table = GetTable(null);
            Document doc;
            if (rangeKey == null)
            {
                doc = await table.GetItemAsync(hashKey, cancellationToken);
            }
            else
            {
                doc = await table.GetItemAsync(hashKey, rangeKey, cancellationToken);
            }
            return AuditEvent.FromJson<T>(doc.ToJson());
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="hashKey">The primary key Hash portion</param>
        /// <param name="rangeKey">The primary key Range portion, if any. Otherwise NULL.</param>
        public T GetEvent<T>(DynamoDBEntry hashKey, DynamoDBEntry rangeKey) where T : AuditEvent
        {
            return GetEvent<T>(hashKey?.AsPrimitive(), rangeKey?.AsPrimitive());
        }

        /// <summary>
        /// Asynchronously gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="hashKey">The primary key Hash portion</param>
        /// <param name="rangeKey">The primary key Range portion, if any. Otherwise NULL.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public async Task<T> GetEventAsync<T>(DynamoDBEntry hashKey, DynamoDBEntry rangeKey, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            return await GetEventAsync<T>(hashKey?.AsPrimitive(), rangeKey?.AsPrimitive(), cancellationToken);
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a DynamoDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        public override T GetEvent<T>(object eventId) 
        {
            if (eventId == null)
            {
                return null;
            }
            if (eventId is Primitive[] keys)
            {
                return GetEvent<T>(keys[0], keys.Length > 1 ? keys[1] : null);
            }
            if (eventId is Primitive key)
            {
                return GetEvent<T>(key, null);
            }
            if (eventId is DynamoDBEntry[] ekeys)
            {
                return GetEvent<T>(ekeys[0], ekeys.Length > 1 ? ekeys[1] : null);
            }
            if (eventId is DynamoDBEntry ekey)
            {
                return GetEvent<T>(ekey, null);
            }
            throw new ArgumentException("Parameter must be convertible to Primitive, Primitive[], DynamoDBEntry or DynamoDBEntry[]", "eventId");
        }

        /// <summary>
        /// Asynchronously gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a DynamoDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            if (eventId == null)
            {
                return null;
            }
            if (eventId is Primitive[] keys)
            {
                return await GetEventAsync<T>(keys[0], keys.Length > 1 ? keys[1] : null, cancellationToken);
            }
            if (eventId is Primitive key)
            {
                return await GetEventAsync<T>(key, null, cancellationToken);
            }
            if (eventId is DynamoDBEntry[] ekeys)
            {
                return await GetEventAsync<T>(ekeys[0], ekeys.Length > 1 ? ekeys[1] : null, cancellationToken);
            }
            if (eventId is DynamoDBEntry ekey)
            {
                return await GetEventAsync<T>(ekey, null, cancellationToken);
            }
            throw new ArgumentException("Parameter must be convertible to Primitive, Primitive[], DynamoDBEntry or DynamoDBEntry[]", "eventId");
        }

        private Table GetTable(AuditEvent auditEvent)
        {
            var tableName = TableNameBuilder?.Invoke(auditEvent) ?? auditEvent?.GetType().Name ?? "AuditEvent";
            if (TableCache.TryGetValue(tableName, out Table table))
            {
                return table;
            }
            table = Table.LoadTable(Client.Value, tableName);
            TableCache[tableName] = table;
            return table;
        }

        private Primitive[] GetKeyValues(Document document, Table table)
        {
            var keyValues = new List<Primitive>() { document[table.HashKeys[0]].AsPrimitive() };
            if (table.RangeKeys.Count > 0)
            {
                keyValues.Add(document[table.RangeKeys[0]].AsPrimitive());
            }
            return keyValues.ToArray();
        }

        private Document CreateDocument(AuditEvent auditEvent, bool addCustomFields)
        {
            if (addCustomFields && CustomAttributes != null)
            {
                foreach (var attrib in CustomAttributes)
                {
                    auditEvent.CustomFields[attrib.Key] = attrib.Value.Invoke(auditEvent);
                }
            }
            return Document.FromJson(auditEvent.ToJson());
        }
    }
}
