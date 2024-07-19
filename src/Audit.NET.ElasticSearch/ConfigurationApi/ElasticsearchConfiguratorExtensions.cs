using System;
using Audit.Core.ConfigurationApi;
using Audit.Elasticsearch.Configuration;
using Audit.Elasticsearch.Providers;

namespace Audit.Core
{
    public static class ElasticsearchConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in Elasticsearch indexes.
        /// </summary>
        /// <param name="config">The Elasticsearch provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseElasticsearch(this IConfigurator configurator, Action<IElasticsearchProviderConfigurator> config)
        {
            Configuration.DataProvider = new ElasticsearchDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
