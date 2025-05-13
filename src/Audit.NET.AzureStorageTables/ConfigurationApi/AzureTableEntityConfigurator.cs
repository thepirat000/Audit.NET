using Audit.Core;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public class AzureTableEntityConfigurator : IAzureTablesEntityConfigurator 
    {
        internal Setting<string> _tableName;
        internal TableClientOptions _clientOptions;
        internal Func<AuditEvent, ITableEntity> _tableEntityBuilder;

        public IAzureTablesEntityConfigurator EntityMapper(Func<AuditEvent, ITableEntity> tableEntityMapper)
        {
            _tableEntityBuilder = tableEntityMapper;
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
            _tableName = tableName;
            return this;
        }

        public IAzureTablesEntityConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableName = tableNameBuilder;
            return this;
        }

        public IAzureTablesEntityConfigurator ClientOptions(TableClientOptions options)
        {
            _clientOptions = options;
            return this;
        }
    }
}
