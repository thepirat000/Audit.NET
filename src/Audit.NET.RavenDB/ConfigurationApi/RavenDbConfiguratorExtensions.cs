using Audit.Core;
using Audit.Core.ConfigurationApi;
using Audit.RavenDB.Providers;
using System;

namespace Audit.RavenDB.ConfigurationApi
{
    public static class RavenDbConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a RavenDB database.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="config">The RavenDB provider configuration.</param>
        public static ICreationPolicyConfigurator UseRavenDB(this IConfigurator configurator, Action<IRavenDbProviderConfigurator> config)
        {
            Configuration.DataProvider = new RavenDbDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
