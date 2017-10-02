using Audit.PostgreSql.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.PostgreSql.Configuration
{
    public class PostgreSqlProviderConfigurator : IPostgreSqlProviderConfigurator
    {
        internal string _connectionString = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=admin;Database=postgres;";
        internal string _schema = null;
        internal string _tableName = "event";
        internal string _idColumnName = "id";
        internal string _dataColumnName = "data";
        internal DataType _dataColumnType = DataType.JSON;
        internal string _lastUpdatedColumnName = null;

        public IPostgreSqlProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public IPostgreSqlProviderConfigurator TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public IPostgreSqlProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnName = idColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator DataColumn(string dataColumnName, DataType dataColumnType = DataType.JSON)
        {
            _dataColumnName = dataColumnName;
            _dataColumnType = dataColumnType;
            return this;
        }

        public IPostgreSqlProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnName = lastUpdatedColumnName;
            return this;
        }

        public IPostgreSqlProviderConfigurator Schema(string schema)
        {
            _schema = schema;
            return this;
        }
    }
}