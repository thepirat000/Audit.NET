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
                ConnectionStringBuilder = sqlDbConfig._connectionStringBuilder,
                TableNameBuilder = sqlDbConfig._tableNameBuilder,
                IdColumnNameBuilder = sqlDbConfig._idColumnNameBuilder,
                JsonColumnNameBuilder = sqlDbConfig._jsonColumnNameBuilder,
                LastUpdatedDateColumnNameBuilder = sqlDbConfig._lastUpdatedColumnNameBuilder,
                SchemaBuilder = sqlDbConfig._schemaBuilder,
                CustomColumns = sqlDbConfig._customColumns,
#if NET45
                SetDatabaseInitializerNull = sqlDbConfig._setDatabaseInitializerNull
#else
                DbContextOptionsBuilder = sqlDbConfig._dbContextOptionsBuilder
#endif
            };
            return new CreationPolicyConfigurator();
        }
    }
}