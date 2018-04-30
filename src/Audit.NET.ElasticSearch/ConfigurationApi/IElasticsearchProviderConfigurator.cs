using System;
using Audit.Core;
using Audit.Elasticsearch.Providers;
using Elastic = Elasticsearch.Net;
using Nest;

namespace Audit.Elasticsearch.Configuration
{
    /// <summary>
    /// Provides a configuration for the Elasticsearch data provider
    /// </summary>
    public interface IElasticsearchProviderConfigurator
    {
        /// <summary>
        /// Specifies the Elasticsearch connection settings.
        /// </summary>
        /// <param name="connectionSettings">The elasticsearch connection settings.</param>
        IElasticsearchProviderConfigurator ConnectionSettings(AuditConnectionSettings connectionSettings);

        /// <summary>
        /// Specifies the Elasticsearch connection settings by providing the single node URL.
        /// </summary>
        /// <param name="uri">The elasticsearch single node URL.</param>
        IElasticsearchProviderConfigurator ConnectionSettings(Uri uri);

        /// <summary>
        /// Specifies the Elasticsearch connection settings by providing the connection pool.
        /// </summary>
        /// <param name="connectionPool">The connection pool to use.</param>
        IElasticsearchProviderConfigurator ConnectionSettings(Elastic.IConnectionPool connectionPool);

        /// <summary>
        /// Specifies the Elasticsearch connection settings by providing the connection pool and the connection.
        /// </summary>
        /// <param name="connectionPool">The connection pool to use.</param>
        /// <param name="connection">The connection to use.</param>
        IElasticsearchProviderConfigurator ConnectionSettings(Elastic.IConnectionPool connectionPool, Elastic.IConnection connection);

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

        /// <summary>
        /// Specifies the Elasticsearch document type to use for an audit event.
        /// NOTE: Mapping types will be completely removed in Elasticsearch 7.0.0.
        /// </summary>
        /// <param name="typeNameBuilder">The builder to get the type to use for an audit event.</param>
        [Obsolete("Mapping types will be completely removed in Elasticsearch 7.0.0.")]
        IElasticsearchProviderConfigurator Type(Func<AuditEvent, TypeName> typeNameBuilder);

        /// <summary>
        /// Specifies the Elasticsearch document type to use for an audit event.
        /// NOTE: Mapping types will be completely removed in Elasticsearch 7.0.0.
        /// </summary>
        /// <param name="typeName">The type to use for the audit events.</param>
        [Obsolete("Mapping types will be completely removed in Elasticsearch 7.0.0.")]
        IElasticsearchProviderConfigurator Type(string typeName);
    }
}
