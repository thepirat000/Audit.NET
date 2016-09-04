using System;
using Audit.SqlServer.Providers;
using Audit.Core.Configuration;
using Audit.SqlServer.Configuration;

namespace Audit.Core
{
    public static class SqlServerConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a Sql Server database.
        /// </summary>
        /// <param name="connectionString">The Sql Server connection string.</param>
        /// <param name="tableName">The Sql table name to store the events.</param>
        /// <param name="idColumnName">The primary key column name.</param>
        /// <param name="jsonColumnName">The column name where to store the json data.</param>
        /// <param name="lastUpdatedDateColumnName">The column name where to store the last updated date.</param>
        public static ICreationPolicyConfigurator UseSqlServer(this IConfigurator configurator, string connectionString,
            string tableName = "Event", string idColumnName = "Id", string jsonColumnName = "Data", string lastUpdatedDateColumnName = null)
        {
            AuditConfiguration.SetDataProvider(new SqlDataProvider()
            {
                ConnectionString = connectionString,
                TableName = tableName,
                IdColumnName = idColumnName,
                JsonColumnName = jsonColumnName,
                LastUpdatedDateColumnName = lastUpdatedDateColumnName
            });
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in a Sql Server database.
        /// </summary>
        /// <param name="config">The Sql Serevr provider configuration.</param>
        public static ICreationPolicyConfigurator UseSqlServer(this IConfigurator configurator, Action<ISqlServerProviderConfigurator> config)
        {
            var sqlDbConfig = new SqlServerProviderConfigurator();
            config.Invoke(sqlDbConfig);
            return UseSqlServer(configurator, sqlDbConfig._connectionString, sqlDbConfig._tableName, 
                sqlDbConfig._idColumnName, sqlDbConfig._jsonColumnName, sqlDbConfig._lastUpdatedColumnName);
        }
    }
}