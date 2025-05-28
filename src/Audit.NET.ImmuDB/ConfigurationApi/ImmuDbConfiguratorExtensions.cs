using System;
using Audit.Core.ConfigurationApi;
using Audit.ImmuDB.ConfigurationApi;
using Audit.ImmuDB.Providers;

namespace Audit.Core
{
    public static class ImmuDbConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a ImmuDB database.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="config">The Immu DB provider configuration.</param>
        public static ICreationPolicyConfigurator UseImmuDb(this IConfigurator configurator, Action<IImmuDbProviderConfigurator> config)
        {
            Configuration.DataProvider = new ImmuDbDataProvider(config);

            return new CreationPolicyConfigurator();
        }
    }
}
