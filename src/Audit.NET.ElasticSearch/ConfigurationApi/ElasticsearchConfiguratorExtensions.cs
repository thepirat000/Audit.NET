using System;
using Audit.Core.ConfigurationApi;
using Audit.Elasticsearch.Configuration;
using Audit.Elasticsearch.Providers;
using Nest;

namespace Audit.Core
{
    public static class ElasticsearchConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in Elasticsearch indexes.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object</param>
        /// <param name="settings">The Elasticsearch connection settings</param>
        /// <param name="idBuilder">The builder to get the id to use for an audit event. NULL to use a server generated id</param>
        /// <param name="indexBuilder">The builder to get the index to use for an audit event. NULL to use the default index name</param>
        public static ICreationPolicyConfigurator UseElasticsearch(this IConfigurator configurator, IConnectionSettingsValues settings,
            Func<AuditEvent, Id> idBuilder = null, Func<AuditEvent, IndexName> indexBuilder = null)
        {
            Configuration.DataProvider = new ElasticsearchDataProvider()
            {
                ConnectionSettings = settings,
                IdBuilder = idBuilder,
                IndexBuilder = indexBuilder
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in Elasticsearch indexes.
        /// </summary>
        /// <param name="config">The Elasticsearch provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseElasticsearch(this IConfigurator configurator, Action<IElasticsearchProviderConfigurator> config)
        {
            var elasDbConfig = new ElasticsearchProviderConfigurator();
            config.Invoke(elasDbConfig);
            Configuration.DataProvider = new ElasticsearchDataProvider()
            {
                ConnectionSettings = elasDbConfig._connectionSettings,
                IdBuilder = elasDbConfig._idBuilder,
                IndexBuilder = elasDbConfig._indexBuilder
            };
            return new CreationPolicyConfigurator();
        }
    }
}
