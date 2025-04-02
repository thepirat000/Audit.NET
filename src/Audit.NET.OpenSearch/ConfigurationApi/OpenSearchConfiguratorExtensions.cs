using System;
using Audit.Core.ConfigurationApi;
using Audit.OpenSearch.Configuration;
using Audit.OpenSearch.Providers;

namespace Audit.Core
{
    public static class OpenSearchConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in OpenSearch indexes.
        /// </summary>
        /// <param name="config">The OpenSearch provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseOpenSearch(this IConfigurator configurator, Action<IOpenSearchProviderConfigurator> config)
        {
            Configuration.DataProvider = new OpenSearchDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
