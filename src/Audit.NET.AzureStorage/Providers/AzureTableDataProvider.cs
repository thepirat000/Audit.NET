using Audit.AzureTableStorage.ConfigurationApi;
using Audit.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.AzureTableStorage.Providers
{
    /// <summary>
    /// Data provider for storing audit events in Azure Tables
    /// </summary>
    public class AzureTableDataProvider : AuditDataProvider
    {
        private static readonly IDictionary<string, CloudTable> TableCache = new ConcurrentDictionary<string, CloudTable>();

        /// <summary>
        /// Gets or sets a function that returns a table name for the event.
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; }

        /// <summary>
        /// Sets the table name to use.
        /// </summary>
        public string TableName { set { TableNameBuilder = _ => value; } }

        /// <summary>
        /// Gets or sets a function that returns a Table Entity from an Audit Event.
        /// </summary>
        public Func<AuditEvent, ITableEntity> TableEntityMapper { get; set; }

        /// <summary>
        /// Gets or sets the Azure Storage connection string
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder { get; set; }

        /// <summary>
        /// Sets the Azure Storage connection string
        /// </summary>
        public string ConnectionString { set { ConnectionStringBuilder = _ => value; } }

        public AzureTableDataProvider()
        {

        }

        public AzureTableDataProvider(Action<IAzureTableProviderConfigurator> config)
        {
            var azConfig = new AzureTableProviderConfigurator();
            if (config != null)
            {
                config.Invoke(azConfig);
                TableNameBuilder = azConfig._tableNameBuilder;
                TableEntityMapper = azConfig._tableEntityBuilder;
                ConnectionStringBuilder = azConfig._connectionStringBuilder;
            }
        }

        #region Overrides

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var table = EnsureTable(auditEvent);
            var entity = GetTableEntity(auditEvent);
            table.ExecuteAsync(TableOperation.Insert(entity)).GetAwaiter().GetResult();
            return new [] { entity.PartitionKey, entity.RowKey };
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var table = EnsureTable(auditEvent);
            var entity = GetTableEntity(auditEvent);
            await table.ExecuteAsync(TableOperation.Insert(entity));
            return new [] { entity.PartitionKey, entity.RowKey };
        }
                
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var fields = eventId as string[];
            var partKey = fields[0];
            var rowKey = fields[1];
            var table = EnsureTable(auditEvent);
            var entity = GetTableEntity(auditEvent);
            entity.PartitionKey = partKey;
            entity.RowKey = rowKey;
            entity.ETag = "*";
            table.ExecuteAsync(TableOperation.Replace(entity)).GetAwaiter().GetResult();
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var fields = eventId as string[];
            var partKey = fields[0];
            var rowKey = fields[1];
            var table = EnsureTable(auditEvent);
            var entity = GetTableEntity(auditEvent);
            entity.PartitionKey = partKey;
            entity.RowKey = rowKey;
            entity.ETag = "*";
            await table.ExecuteAsync(TableOperation.Replace(entity));
        }

        #endregion

        #region Private Methods

        private ITableEntity GetTableEntity(AuditEvent auditEvent)
        {
            if (TableEntityMapper != null)
            {
                return TableEntityMapper.Invoke(auditEvent);
            }
            return new AuditEventTableEntity(
                auditEvent.GetType().Name, 
                Guid.NewGuid().ToString(),
                auditEvent);
        }

        private string GetTableName(AuditEvent auditEvent)
        {
            if (TableNameBuilder != null)
            {
                return TableNameBuilder.Invoke(auditEvent);
            }
            return "event";
        }

        private string GetConnectionString(AuditEvent auditEvent)
        {
            return ConnectionStringBuilder?.Invoke(auditEvent);
        }

        internal CloudTable EnsureTable(AuditEvent auditEvent)
        {
            var cnnString = GetConnectionString(auditEvent);
            var tableName = GetTableName(auditEvent);
            return EnsureTable(cnnString, tableName);
        }

        internal async Task<CloudTable> EnsureTableAsync(AuditEvent auditEvent)
        {
            var cnnString = GetConnectionString(auditEvent);
            var tableName = GetTableName(auditEvent);
            return await EnsureTableAsync(cnnString, tableName);
        }

        internal CloudTable EnsureTable(string cnnString, string tableName)
        {
            CloudTable result;
            var cacheKey = cnnString + "|" + tableName;
            if (TableCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExistsAsync().Wait();
            TableCache[cacheKey] = table;
            return table;
        }

        internal async Task<CloudTable> EnsureTableAsync(string cnnString, string tableName)
        {
            CloudTable result;
            var cacheKey = cnnString + "|" + tableName;
            if (TableCache.TryGetValue(cacheKey, out result))
            {
                return result;
            }
            var storageAccount = CloudStorageAccount.Parse(cnnString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            TableCache[cacheKey] = table;
            return table;
        }
        #endregion
    }
}
