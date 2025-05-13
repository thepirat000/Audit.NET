using System;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureTableProviderConfigurator : IAzureTableProviderConfigurator
    {
        internal Func<AuditEvent, string> _connectionStringBuilder = null;
        internal Func<AuditEvent, string> _tableNameBuilder = null;
        internal Func<AuditEvent, ITableEntity> _tableEntityBuilder = null;

        public IAzureTableProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionStringBuilder = _ => connectionString;
            return this;
        }

        public IAzureTableProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
            return this;
        }

        public IAzureTableProviderConfigurator EntityMapper(Func<AuditEvent, ITableEntity> tableEntityMapper)
        {
            _tableEntityBuilder = tableEntityMapper;
            return this;
        }

        public IAzureTableProviderConfigurator EntityBuilder(Action<IAzureTableEntityConfigurator> entityConfigurator)
        {
            var config = new AzureTableEntityConfigurator();
            entityConfigurator.Invoke(config);
            _tableEntityBuilder = ev => new DynamicTableEntity(
                config._partKeyBuilder?.Invoke(ev) ?? "event", 
                config._rowKeyBuilder?.Invoke(ev) ?? Guid.NewGuid().ToString(), 
                null,
                config._propsBuilder?.Invoke(ev));
            return this;
        }

        public IAzureTableProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return this;
        }

        public IAzureTableProviderConfigurator TableName(string tableName)
        {
            _tableNameBuilder = _ => tableName;
            return this;
        }


    }
}