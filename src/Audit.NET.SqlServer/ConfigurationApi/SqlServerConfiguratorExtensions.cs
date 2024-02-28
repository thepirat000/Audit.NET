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
            Configuration.DataProvider = new SqlDataProvider(config);

            return new CreationPolicyConfigurator();
        }
    }
}