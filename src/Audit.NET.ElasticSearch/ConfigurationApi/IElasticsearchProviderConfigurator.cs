using System;
using Audit.Core;
using Elastic.Clients.Elasticsearch;

namespace Audit.Elasticsearch.Configuration
{
    /// <summary>
    /// Provides a configuration for the Elasticsearch data provider
    /// </summary>
    public interface IElasticsearchProviderConfigurator
    {
        /// <summary>
        /// Specifies the Elasticsearch client settings.
        /// </summary>
        /// <param name="clientSettings">The elasticsearch client settings.</param>
        IElasticsearchProviderConfigurator Client(IElasticsearchClientSettings clientSettings);

        /// <summary>
        /// Specifies the Elasticsearch client to use. 
        /// </summary>
        /// <param name="client">The elasticsearch client to use.</param>
        IElasticsearchProviderConfigurator Client(ElasticsearchClient client);

        /// <summary>
        /// Specifies the Elasticsearch client settings by providing a single node URL.
        /// </summary>
        /// <param name="uri">The elasticsearch single node URL.</param>
        IElasticsearchProviderConfigurator Client(Uri uri);

        /// <summary>
        /// Specifies the Elasticsearch Index name to use.
        /// </summary>
        /// <param name="indexName">The index name to use. NULL to use the default index name.</param>
        IElasticsearchProviderConfigurator Index(string indexName);
        /// <summary>
        /// Specifies the Elasticsearch Index name to use for an audit event.
        /// </summary>
        /// <param name="indexNameBuilder">The builder to get the index to use for an audit event. NULL to use the default index name.</param>
        IElasticsearchProviderConfigurator Index(Func<AuditEvent, IndexName> indexNameBuilder);

        /// <summary>
        /// Specifies the Elasticsearch document Id to use for an audit event.
        /// </summary>
        /// <param name="idBuilder">The builder to get the id to use for an audit event. NULL to use a server generated id.</param>
        IElasticsearchProviderConfigurator Id(Func<AuditEvent, Id> idBuilder);
    }
}
