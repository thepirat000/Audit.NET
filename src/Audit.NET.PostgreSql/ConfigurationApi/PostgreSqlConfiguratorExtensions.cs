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
            Configuration.DataProvider = new PostgreSqlDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}