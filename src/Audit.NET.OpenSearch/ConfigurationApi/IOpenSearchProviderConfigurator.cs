using System;
using Audit.Core;
using OpenSearch.Client;

namespace Audit.OpenSearch.Configuration
{
    /// <summary>
    /// Provides a configuration for the OpenSearch data provider
    /// </summary>
    public interface IOpenSearchProviderConfigurator
    {
        /// <summary>
        /// Specifies the OpenSearch client settings.
        /// </summary>
        /// <param name="clientSettings">The OpenSearch client settings.</param>
        IOpenSearchProviderConfigurator Client(IConnectionSettingsValues clientSettings);

        /// <summary>
        /// Specifies the OpenSearch client to use. 
        /// </summary>
        /// <param name="client">The OpenSearch client to use.</param>
        IOpenSearchProviderConfigurator Client(OpenSearchClient client);

        /// <summary>
        /// Specifies the OpenSearch client settings by providing a single node URL.
        /// </summary>
        /// <param name="uri">The OpenSearch single node URL.</param>
        IOpenSearchProviderConfigurator Client(Uri uri);

        /// <summary>
        /// Specifies the OpenSearch Index name to use.
        /// </summary>
        /// <param name="indexName">The index name to use. NULL to use the default index name.</param>
        IOpenSearchProviderConfigurator Index(string indexName);
        /// <summary>
        /// Specifies the OpenSearch Index name to use for an audit event.
        /// </summary>
        /// <param name="indexNameBuilder">The builder to get the index to use for an audit event. NULL to use the default index name.</param>
        IOpenSearchProviderConfigurator Index(Func<AuditEvent, IndexName> indexNameBuilder);

        /// <summary>
        /// Specifies the OpenSearch document Id to use for an audit event.
        /// </summary>
        /// <param name="idBuilder">The builder to get the id to use for an audit event. NULL to use a server generated id.</param>
        IOpenSearchProviderConfigurator Id(Func<AuditEvent, Id> idBuilder);
    }
}
