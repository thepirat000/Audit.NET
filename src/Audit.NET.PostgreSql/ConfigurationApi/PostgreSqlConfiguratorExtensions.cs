using System;
using Audit.Core.ConfigurationApi;
using Audit.PostgreSql.Configuration;
using Audit.PostgreSql.Providers;

namespace Audit.Core
{
    public static class PostgreSqlConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a PostgreSQL database.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">The table name to store the events.</param>
        /// <param name="idColumnName">The primary key column name.</param>
        /// <param name="dataColumnName">The column name where to store the event data.</param>
        /// <param name="dataColumnType">The type of the data column.</param>
        /// <param name="lastUpdatedDateColumnName">The column name where to store the last updated date.</param>
        /// <param name="schema">The schema name to use when storing the events.</param>
        public static ICreationPolicyConfigurator UsePostgreSql(this IConfigurator configurator, string connectionString,
            string tableName = "event", string idColumnName = "id", string dataColumnName = "data", DataType dataColumnType = DataType.JSON, 
            string lastUpdatedDateColumnName = null, string schema = null)
        {
            Configuration.DataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = connectionString,
                TableName = tableName,
                IdColumnName = idColumnName,
                DataColumnName = dataColumnName,
                DataType = dataColumnType == DataType.String ? null : dataColumnType.ToString(),
                LastUpdatedDateColumnName = lastUpdatedDateColumnName,
                Schema = schema
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in a PostgreSQL database.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="config">The PostgreSQL provider configuration.</param>
        public static ICreationPolicyConfigurator UsePostgreSql(this IConfigurator configurator, Action<IPostgreSqlProviderConfigurator> config)
        {
            var pgDbConfig = new PostgreSqlProviderConfigurator();
            config.Invoke(pgDbConfig);
            return UsePostgreSql(configurator, pgDbConfig._connectionString, pgDbConfig._tableName,
                pgDbConfig._idColumnName, pgDbConfig._dataColumnName, pgDbConfig._dataColumnType, pgDbConfig._lastUpdatedColumnName, pgDbConfig._schema);
        }
    }
}