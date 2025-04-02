using System;
using Audit.Core;
using OpenSearch.Client;

namespace Audit.OpenSearch.Configuration
{
    public class OpenSearchProviderConfigurator : IOpenSearchProviderConfigurator
    {
        internal IConnectionSettingsValues _clientSettings;
        internal Setting<IndexName> _index;
        internal Func<Core.AuditEvent, Id> _idBuilder;
        internal OpenSearchClient _client;

        public IOpenSearchProviderConfigurator Client(IConnectionSettingsValues clientSettings)
        {
            _clientSettings = clientSettings;
            _client = null;
            return this;
        }

        public IOpenSearchProviderConfigurator Client(OpenSearchClient client)
        {
            _client = client;
            _clientSettings = null;
            return this;
        }

        public IOpenSearchProviderConfigurator Client(Uri uri)
        {
            _clientSettings = new ConnectionSettings(uri);
            _client = null;
            return this;
        }
        
        public IOpenSearchProviderConfigurator Id(Func<Core.AuditEvent, Id> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }

        public IOpenSearchProviderConfigurator Index(string indexName)
        {
            _index = (IndexName)indexName;
            return this;
        }

        public IOpenSearchProviderConfigurator Index(Func<Core.AuditEvent, IndexName> indexNameBuilder)
        {
            _index = indexNameBuilder;
            return this;
        }

    }
}
