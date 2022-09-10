using Audit.Core;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public class AzureTableEntityConfigurator : IAzureTablesEntityConfigurator 
    {
        internal Func<AuditEvent, string> _tableNameBuilder;
        internal TableClientOptions _clientOptions;
        internal Func<AuditEvent, ITableEntity> _tableEntityBuilder = null;

        public IAzureTablesEntityConfigurator EntityMapper(Func<AuditEvent, ITableEntity> tableEntityBuilder)
        {
            _tableEntityBuilder = tableEntityBuilder;
            return this;
        }

        public IAzureTablesEntityConfigurator EntityBuilder(Action<IAzureTableRowConfigurator> entityConfigurator)
        {
            var config = new AzureTableRowConfigurator();
            entityConfigurator.Invoke(config);
            _tableEntityBuilder = ev => new TableEntity(config._propsBuilder?.Invoke(ev))
            {
                PartitionKey = config._partKeyBuilder?.Invoke(ev) ?? "event",
                RowKey = config._rowKeyBuilder?.Invoke(ev) ?? Guid.NewGuid().ToString()
            };
            return this;
        }

        public IAzureTablesEntityConfigurator TableName(string tableName)
        {
            _tableNameBuilder = _ => tableName;
            return this;
        }

        public IAzureTablesEntityConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return this;
        }

        public IAzureTablesEntityConfigurator ClientOptions(TableClientOptions options)
        {
            _clientOptions = options;
            return this;
        }
    }
}
