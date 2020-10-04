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
                ConnectionStringBuilder = pgDbConfig._connectionStringBuilder,
                TableNameBuilder = pgDbConfig._tableNameBuilder,
                IdColumnNameBuilder = pgDbConfig._idColumnNameBuilder,
                DataColumnNameBuilder = pgDbConfig._dataColumnNameBuilder,
                DataType = pgDbConfig._dataColumnType == DataType.String ? null : pgDbConfig._dataColumnType.ToString(),
                LastUpdatedDateColumnNameBuilder = pgDbConfig._lastUpdatedColumnNameBuilder,
                SchemaBuilder = pgDbConfig._schemaBuilder,
                CustomColumns = pgDbConfig._customColumns
            };
            return new CreationPolicyConfigurator();
        }
    }
}