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
        /// <param name="config">The PostgreSQL provider configuration.</param>
        public static ICreationPolicyConfigurator UsePostgreSql(this IConfigurator configurator, Action<IPostgreSqlProviderConfigurator> config)
        {
            var pgDbConfig = new PostgreSqlProviderConfigurator();
            config.Invoke(pgDbConfig);

            Configuration.DataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = pgDbConfig._connectionString,
                TableName = pgDbConfig._tableName,
                IdColumnName = pgDbConfig._idColumnName,
                DataColumnName = pgDbConfig._dataColumnName,
                DataType = pgDbConfig._dataColumnType == DataType.String ? null : pgDbConfig._dataColumnType.ToString(),
                LastUpdatedDateColumnName = pgDbConfig._lastUpdatedColumnName,
                Schema = pgDbConfig._schema,
                CustomColumns = pgDbConfig._customColumns
            };
            return new CreationPolicyConfigurator();
        }
    }
}