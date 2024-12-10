using System;
using System.Collections.Generic;
using System.Data.Common;
using Audit.Core;
using Microsoft.EntityFrameworkCore;

namespace Audit.SqlServer.Configuration
{
    [CLSCompliant(false)]
    public class SqlServerProviderConfigurator : ISqlServerProviderConfigurator
    {
        internal Setting<string> _connectionString =  "data source=localhost;initial catalog=Audit;integrated security=true;Encrypt=False;";
        internal Setting<DbConnection> _dbConnection;
        internal Setting<DbContext> _dbContext;
        internal Setting<string> _schema = (string)null;
        internal Setting<string> _tableName = "Event";
        internal Setting<string> _idColumnName = "Id";
        internal Setting<string> _jsonColumnName;
        internal Setting<string> _lastUpdatedColumnName = (string)null;
        internal List<CustomColumn> _customColumns = new List<CustomColumn>();
        internal Setting<DbContextOptions> _dbContextOptions = (DbContextOptions)null;

        public ISqlServerProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public ISqlServerProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionString = connectionStringBuilder;
            _dbConnection = new();
            _dbContext = new();
            return this;
        }

        public ISqlServerProviderConfigurator DbConnection(Func<AuditEvent, DbConnection> dbConnection)
        {
            _connectionString = new();
            _dbConnection = dbConnection;
            _dbContext = new();
            return this;
        }

        public ISqlServerProviderConfigurator DbContext(Func<AuditEvent, DbContext> dbContext)
        {
            _connectionString = new();
            _dbConnection = new();
            _dbContext = dbContext;
            return this;
        }

        public ISqlServerProviderConfigurator TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public ISqlServerProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableName = tableNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnName = idColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator IdColumnName(Func<AuditEvent, string> idColumnNameBuilder)
        {
            _idColumnName = idColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator JsonColumnName(string jsonColumnName)
        {
            _jsonColumnName = jsonColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator JsonColumnName(Func<AuditEvent, string> jsonColumnNameBuilder)
        {
            _jsonColumnName = jsonColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnName = lastUpdatedColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator LastUpdatedColumnName(Func<AuditEvent, string> lastUpdatedColumnNameBuilder)
        {
            _lastUpdatedColumnName = lastUpdatedColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator Schema(string schema)
        {
            _schema = schema;
            return this;
        }

        public ISqlServerProviderConfigurator Schema(Func<AuditEvent, string> schemaBuilder)
        {
            _schema = schemaBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value)
        {
            _customColumns.Add(new CustomColumn(columnName, value));
            return this;
        }

        public ISqlServerProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value, Func<AuditEvent, bool> guard)
        {
            _customColumns.Add(new CustomColumn(columnName, value, guard));
            return this;
        }

        public ISqlServerProviderConfigurator DbContextOptions(Func<AuditEvent, DbContextOptions> dbContextOptionsBuilder)
        {
            _dbContextOptions = dbContextOptionsBuilder;
            return this;
        }
        public ISqlServerProviderConfigurator DbContextOptions(DbContextOptions dbContextOptions)
        {
            _dbContextOptions = dbContextOptions;
            return this;
        }
    }
}
