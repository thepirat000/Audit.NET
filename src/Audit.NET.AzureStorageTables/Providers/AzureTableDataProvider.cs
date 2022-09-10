using Audit.AzureStorageTables.ConfigurationApi;
using Audit.Core;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Audit.AzureStorageTables.Providers
{
    public class AzureTableDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Azure Tables connection string
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Azure Tables table name builder
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; }
        /// <summary>
        /// The Azure.Data.Tables Client Options to use
        /// </summary>
        public TableClientOptions ClientOptions { get; set; }
        /// <summary>
        /// The Service endpoint to connect to. Alternative to ConnectionString.
        /// </summary>
        public Uri ServiceEndpoint { get; set; }
        /// <summary>
        /// The Shared Key credential to use to connect to the Service.
        /// </summary>
        public TableSharedKeyCredential SharedKeyCredential { get; set; }
        /// <summary>
        /// The Sas credential to use to connect to the Service.
        /// </summary>
        public AzureSasCredential SasCredential { get; set; }
        /// <summary>
        /// The Token credential to use to connect to the Service.
        /// </summary>
        public TokenCredential TokenCredential { get; set; }
        /// <summary>
        /// Gets or sets a function that returns a Table Entity from an Audit Event.
        /// </summary>
        public Func<AuditEvent, ITableEntity> TableEntityMapper { get; set; }
        /// <summary>
        /// Provides a factory to create the TableClient. Alternative to customize the table client creation.
        /// </summary>
        public Func<AuditEvent, TableClient> TableClientFactory { get; set; }

        private static readonly ConcurrentDictionary<string, TableClient> TableClientCache = new ConcurrentDictionary<string, TableClient>();

        public AzureTableDataProvider()
        {
        }
        
        public AzureTableDataProvider(Action<IAzureTableConnectionConfigurator> config)
        {
            var cfg = new AzureTableConnectionConfigurator();
            config.Invoke(cfg);
            
            if (cfg._clientFactory != null)
            {
                // Factory provided
                TableClientFactory = cfg._clientFactory;
            }
            else if (cfg._connectionString != null)
            {
                // By connection string
                ConnectionString = cfg._connectionString;
                ClientOptions = cfg._tableConfig._clientOptions;
                TableNameBuilder = cfg._tableConfig._tableNameBuilder;
            }
            else if (cfg._endpointUri != null)
            {
                // By endpoint
                ServiceEndpoint = cfg._endpointUri;
                ClientOptions = cfg._tableConfig._clientOptions;
                TableNameBuilder = cfg._tableConfig._tableNameBuilder;
                SharedKeyCredential = cfg._sharedKeyCredential;
                SasCredential = cfg._sasCredential;
                TokenCredential = cfg._tokenCredential;
            }

            TableEntityMapper = cfg._tableConfig._tableEntityBuilder;
        }
        
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetTableClient(auditEvent);
            var entity = CreateTableEntity(auditEvent);
            client.AddEntity(entity);
            return new [] { entity.PartitionKey, entity.RowKey };
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var client = await GetTableClientAsync(auditEvent);
            var entity = CreateTableEntity(auditEvent);
            await client.AddEntityAsync(entity);
            return new[] { entity.PartitionKey, entity.RowKey };
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var fields = eventId as string[];
            var partKey = fields[0];
            var rowKey = fields[1];

            var client = GetTableClient(auditEvent);
            var entity = CreateTableEntity(auditEvent);
                        
            entity.PartitionKey = partKey;
            entity.RowKey = rowKey;
            client.UpdateEntity(entity, ETag.All, TableUpdateMode.Replace);
        }
        
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var fields = eventId as string[];
            var partKey = fields[0];
            var rowKey = fields[1];

            var client = await GetTableClientAsync(auditEvent);
            var entity = CreateTableEntity(auditEvent);

            entity.PartitionKey = partKey;
            entity.RowKey = rowKey;
            await client.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        private ITableEntity CreateTableEntity(AuditEvent auditEvent)
        {
            return TableEntityMapper?.Invoke(auditEvent) ?? new AuditEventTableEntity(auditEvent);
        }

        /// <summary>
        /// Returns a cached instance of a TableClient for the table related to the Audit Event. Creates the Table if it does not exists.
        /// </summary>
        /// <param name="auditEvent">The audit event</param>
        public TableClient GetTableClient(AuditEvent auditEvent)
        {
            // From custom factory
            if (TableClientFactory != null)
            {
                return TableClientFactory.Invoke(auditEvent);
            }

            var tableName = TableNameBuilder?.Invoke(auditEvent) ?? "Audit";

            if (TableClientCache.TryGetValue(tableName, out TableClient client))
            {
                // From Cache
                return client;
            }

            // New client
            var newClient = CreateTableclient(tableName);
            newClient.CreateIfNotExists();
            TableClientCache[tableName] = newClient;
            return newClient;
        }

        /// <summary>
        /// Returns a cached instance of a TableClient for the table related to the Audit Event. Creates the Table if it does not exists.
        /// </summary>
        /// <param name="auditEvent">The audit event</param>
        public async Task<TableClient> GetTableClientAsync(AuditEvent auditEvent)
        {
            // From custom factory
            if (TableClientFactory != null)
            {
                return TableClientFactory.Invoke(auditEvent);
            }

            var tableName = TableNameBuilder?.Invoke(auditEvent) ?? "Audit";

            if (TableClientCache.TryGetValue(tableName, out TableClient client))
            {
                // From Cache
                return client;
            }

            // New client
            var newClient = CreateTableclient(tableName);
            await newClient.CreateIfNotExistsAsync();
            TableClientCache[tableName] = newClient;
            return newClient;
        }

        private TableClient CreateTableclient(string tableName)
        {
            if (ConnectionString != null)
            {
                return new TableClient(ConnectionString, tableName, ClientOptions);
            }
            if (ServiceEndpoint != null)
            {
                if (SharedKeyCredential != null)
                {
                    return new TableClient(ServiceEndpoint, tableName, SharedKeyCredential, ClientOptions);
                }
                if (SasCredential != null)
                {
                    return new TableClient(ServiceEndpoint, SasCredential, ClientOptions);
                }
                if (TokenCredential != null)
                {
                    return new TableClient(ServiceEndpoint, tableName, TokenCredential, ClientOptions);
                }
                return new TableClient(ServiceEndpoint, ClientOptions);
            }
            throw new InvalidOperationException("The Azure Tables connection string or endpoint must be provided.");
        }
    }
}
