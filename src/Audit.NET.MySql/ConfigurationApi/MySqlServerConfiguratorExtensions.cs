using System;
using Audit.Core.ConfigurationApi;
using Audit.MySql.Providers;
using Audit.MySql.Configuration;
using Audit.NET.MySql;
using System.Collections.Generic;

namespace Audit.Core
{
    public static class MySqlServerConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a MySQL database.
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <param name="connectionString">The MySQL connection string.</param>
        /// <param name="tableName">The MySQL table name to store the events.</param>
        /// <param name="idColumnName">The primary key column name.</param>
        /// <param name="jsonColumnName">The column name where to store the json data.</param>
        /// <param name="customColumns">The extra custom columns.</param>
        public static ICreationPolicyConfigurator UseMySql(this IConfigurator configurator, string connectionString, 
            string tableName = "event", string idColumnName = "id", 
            string jsonColumnName = "data", List<CustomColumn> customColumns = null)
        {
            Configuration.DataProvider = new MySqlDataProvider()
            {
                ConnectionString = connectionString,
                TableName = tableName,
                IdColumnName = idColumnName,
                JsonColumnName = jsonColumnName,
                CustomColumns = customColumns
            };
            return new CreationPolicyConfigurator();
        }
        /// <summary>
        /// Store the events in a MySQL database.
        /// </summary>
        /// <param name="configurator">The configurator object.</param>
        /// <param name="config">The MySQL provider configuration.</param>
        public static ICreationPolicyConfigurator UseMySql(this IConfigurator configurator, Action<IMySqlServerProviderConfigurator> config)
        {
            var dbConfig = new MySqlServerProviderConfigurator();
            config.Invoke(dbConfig);
            return UseMySql(configurator, dbConfig._connectionString, 
                dbConfig._tableName, dbConfig._idColumnName, 
                dbConfig._jsonColumnName, dbConfig._customColumns);
        }
    }
}