using System;
using Audit.SqlServer.Providers;
using Audit.Core.ConfigurationApi;
using Audit.SqlServer.Configuration;

namespace Audit.Core
{
    public static class SqlServerConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a Sql Server database.
        /// </summary>
        /// <param name="config">The Sql Server provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        [CLSCompliant(false)]
        public static ICreationPolicyConfigurator UseSqlServer(this IConfigurator configurator, Action<ISqlServerProviderConfigurator> config)
        {
            var sqlDbConfig = new SqlServerProviderConfigurator();
            config.Invoke(sqlDbConfig);
            Configuration.DataProvider = new SqlDataProvider()
            {
                ConnectionString = sqlDbConfig._connectionString,
                TableName = sqlDbConfig._tableName,
                IdColumnName = sqlDbConfig._idColumnName,
                JsonColumnName = sqlDbConfig._jsonColumnName,
                LastUpdatedDateColumnName = sqlDbConfig._lastUpdatedColumnName,
                Schema = sqlDbConfig._schema,
                CustomColumns = sqlDbConfig._customColumns,
                DbContextOptions = sqlDbConfig._dbContextOptions
            };
            return new CreationPolicyConfigurator();
        }
    }
}