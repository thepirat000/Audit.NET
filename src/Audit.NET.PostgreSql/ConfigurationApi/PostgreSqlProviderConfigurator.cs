using Audit.Core;
using System;
using System.Collections.Generic;

namespace Audit.PostgreSql.Configuration
{
    public class PostgreSqlProviderConfigurator : IPostgreSqlProviderConfigurator
    {
        internal Setting<string> _connectionString = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=admin;Database=postgres;";
        internal Setting<string> _schema;
        internal Setting<string> _tableName = "event";
        internal Setting<string> _idColumnName = "id";
        internal Setting<string> _dataColumnName = "data";
        internal Func<AuditEvent, string> _dataJsonStringBuilder;
        internal Setting<string> _lastUpdatedColumnName;

        internal DataType _dataColumnType = DataType.JSON;
        internal List<CustomColumn> _customColumns = new List<CustomColumn>();

        public IPostgreSqlProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public IPostgreSqlProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionString = connectionStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public IPostgreSqlProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableName = tableNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnName = idColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator IdColumnName(Func<AuditEvent, string> idColumnNameBuilder)
        {
            _idColumnName = idColumnNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator DataColumn(string dataColumnName, DataType dataColumnType = DataType.JSON, Func<AuditEvent, string> jsonStringBuilder = null)
        {
            _dataColumnName = dataColumnName;
            _dataColumnType = dataColumnType;
            _dataJsonStringBuilder = jsonStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator DataColumn(Func<AuditEvent, string> dataColumnNameBuilder, DataType dataColumnType = DataType.JSON, Func<AuditEvent, string> jsonStringBuilder = null)
        {
            _dataColumnName = dataColumnNameBuilder;
            _dataColumnType = dataColumnType;
            _dataJsonStringBuilder = jsonStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnName = lastUpdatedColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator LastUpdatedColumnName(Func<AuditEvent, string> lastUpdatedColumnNameBuilder)
        {
            _lastUpdatedColumnName = lastUpdatedColumnNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator Schema(string schema)
        {
            _schema = schema;
            return this;
        }

        public IPostgreSqlProviderConfigurator Schema(Func<AuditEvent, string> schemaBuilder)
        {
            _schema = schemaBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value)
        {
            _customColumns.Add(new CustomColumn(columnName, value));
            return this;
        }
    }
}