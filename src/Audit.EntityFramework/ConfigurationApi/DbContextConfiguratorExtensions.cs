#if NET7_0_OR_GREATER
using System;
using Audit.Core.ConfigurationApi;
using Audit.EntityFramework.ConfigurationApi;
using Audit.EntityFramework.Providers;
using Microsoft.EntityFrameworkCore;

namespace Audit.Core
{
    public static class DbContextConfiguratorExtensions
    {
        /// <summary>
        /// Store the audits logs using a generic DbContext provider to store audit events in a single entity/table 
        /// </summary>
        /// <param name="config">The DbContext provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseDbContext<TDbContext, TEntity>(this IConfigurator configurator, Action<IDbContextProviderConfigurator<TDbContext, TEntity>> config)
            where TDbContext : DbContext
            where TEntity : class, new()
        {
            Configuration.DataProvider = new DbContextDataProvider<TDbContext, TEntity>(config);
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the audits logs using Entity Framework Core to store audit events in multiple entities/tables.
        /// </summary>
        /// <param name="config">The DbContext provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseDbContext(this IConfigurator configurator, Action<IDbContextProviderConfigurator> config)
        {
            Configuration.DataProvider = new DbContextDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
#endif