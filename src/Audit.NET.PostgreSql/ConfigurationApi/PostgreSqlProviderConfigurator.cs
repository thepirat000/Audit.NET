using Audit.Core;
using Audit.NET.PostgreSql;
using Audit.PostgreSql.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.PostgreSql.Configuration
{
    public class PostgreSqlProviderConfigurator : IPostgreSqlProviderConfigurator
    {
        internal Func<AuditEvent, string> _connectionStringBuilder = _ => "Server=127.0.0.1;Port=5432;User Id=postgres;Password=admin;Database=postgres;";
        internal Func<AuditEvent, string> _schemaBuilder = _ => null;
        internal Func<AuditEvent, string> _tableNameBuilder = _ => "event";
        internal Func<AuditEvent, string> _idColumnNameBuilder = _ => "id";
        internal Func<AuditEvent, string> _dataColumnNameBuilder = _ => "data";
        internal Func<AuditEvent, string> _dataJsonStringBuilder = null;
        internal Func<AuditEvent, string> _lastUpdatedColumnNameBuilder = _ => null;

        internal DataType _dataColumnType = DataType.JSON;
        internal List<CustomColumn> _customColumns = new List<CustomColumn>();

        public IPostgreSqlProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionStringBuilder = _ => connectionString;
            return this;
        }

        public IPostgreSqlProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator TableName(string tableName)
        {
            _tableNameBuilder = _ => tableName;
            return this;
        }

        public IPostgreSqlProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnNameBuilder = _ => idColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator IdColumnName(Func<AuditEvent, string> idColumnNameBuilder)
        {
            _idColumnNameBuilder = idColumnNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator DataColumn(string dataColumnName, DataType dataColumnType = DataType.JSON, Func<AuditEvent, string> jsonStringBuilder = null)
        {
            _dataColumnNameBuilder = _ => dataColumnName;
            _dataColumnType = dataColumnType;
            _dataJsonStringBuilder = jsonStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator DataColumn(Func<AuditEvent, string> dataColumnNameBuilder, DataType dataColumnType = DataType.JSON, Func<AuditEvent, string> jsonStringBuilder = null)
        {
            _dataColumnNameBuilder = dataColumnNameBuilder;
            _dataColumnType = dataColumnType;
            _dataJsonStringBuilder = jsonStringBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnNameBuilder = _ => lastUpdatedColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator LastUpdatedColumnName(Func<AuditEvent, string> lastUpdatedColumnNameBuilder)
        {
            _lastUpdatedColumnNameBuilder = lastUpdatedColumnNameBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator Schema(string schema)
        {
            _schemaBuilder = _ => schema;
            return this;
        }

        public IPostgreSqlProviderConfigurator Schema(Func<AuditEvent, string> schemaBuilder)
        {
            _schemaBuilder = schemaBuilder;
            return this;
        }

        public IPostgreSqlProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value)
        {
            _customColumns.Add(new CustomColumn(columnName, value));
            return this;
        }
    }
}