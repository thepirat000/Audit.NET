using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
#if NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
using Microsoft.EntityFrameworkCore;
#endif
#if NET45
using System.Data.Common;
#endif

namespace Audit.SqlServer.Configuration
{
    [CLSCompliant(false)]
    public class SqlServerProviderConfigurator : ISqlServerProviderConfigurator
    {
        internal Func<AuditEvent, string> _connectionStringBuilder = ev => "data source=localhost;initial catalog=Audit;integrated security=true;Encrypt=False;";
        internal Func<AuditEvent, string> _schemaBuilder = null;
        internal Func<AuditEvent, string> _tableNameBuilder = ev => "Event";
        internal Func<AuditEvent, string> _idColumnNameBuilder = ev => "Id";
        internal Func<AuditEvent, string> _jsonColumnNameBuilder;
        internal Func<AuditEvent, string> _lastUpdatedColumnNameBuilder = null;
        internal List<CustomColumn> _customColumns = new List<CustomColumn>();
#if NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
        internal Func<AuditEvent, DbContextOptions> _dbContextOptionsBuilder = null;
#endif
#if NET45
        internal bool _setDatabaseInitializerNull = false;
        public ISqlServerProviderConfigurator SetDatabaseInitializerNull(bool initializeToNull = true)
        {
            _setDatabaseInitializerNull = initializeToNull;
            return this;
        }

        internal Func<AuditEvent, DbConnection> _dbConnectionBuilder { get; set; }
        public ISqlServerProviderConfigurator DbConnection(Func<AuditEvent, DbConnection> connection)
        {
            _dbConnectionBuilder = connection;
            return this;
        }

#endif

        public ISqlServerProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionStringBuilder = ev => connectionString;
            return this;
        }

        public ISqlServerProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator TableName(string tableName)
        {
            _tableNameBuilder = ev => tableName;
            return this;
        }

        public ISqlServerProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnNameBuilder = ev => idColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator IdColumnName(Func<AuditEvent, string> idColumnNameBuilder)
        {
            _idColumnNameBuilder = idColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator JsonColumnName(string jsonColumnName)
        {
            _jsonColumnNameBuilder = ev => jsonColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator JsonColumnName(Func<AuditEvent, string> jsonColumnNameBuilder)
        {
            _jsonColumnNameBuilder = jsonColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnNameBuilder = ev => lastUpdatedColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator LastUpdatedColumnName(Func<AuditEvent, string> lastUpdatedColumnNameBuilder)
        {
            _lastUpdatedColumnNameBuilder = lastUpdatedColumnNameBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator Schema(string schema)
        {
            _schemaBuilder = ev => schema;
            return this;
        }

        public ISqlServerProviderConfigurator Schema(Func<AuditEvent, string> schemaBuilder)
        {
            _schemaBuilder = schemaBuilder;
            return this;
        }

        public ISqlServerProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value)
        {
            _customColumns.Add(new CustomColumn(columnName, value));
            return this;
        }
#if NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
        public ISqlServerProviderConfigurator DbContextOptions(Func<AuditEvent, DbContextOptions> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            return this;
        }
        public ISqlServerProviderConfigurator DbContextOptions(DbContextOptions dbContextOptions)
        {
            _dbContextOptionsBuilder = ev => dbContextOptions;
            return this;
        }
#endif
    }
}
