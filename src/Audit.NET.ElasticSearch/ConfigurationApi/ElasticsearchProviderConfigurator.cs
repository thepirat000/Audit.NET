using System;
using Audit.Core;
using Elastic.Clients.Elasticsearch;

namespace Audit.Elasticsearch.Configuration
{
    public class ElasticsearchProviderConfigurator : IElasticsearchProviderConfigurator
    {
        internal IElasticsearchClientSettings _clientSettings;
        internal Setting<IndexName> _index;
        internal Func<Core.AuditEvent, Id> _idBuilder;
        internal ElasticsearchClient _client;

        public IElasticsearchProviderConfigurator Client(ElasticsearchClient client)
        {
            _client = client;
            _clientSettings = null;
            return this;
        }

        public IElasticsearchProviderConfigurator Client(IElasticsearchClientSettings clientSettings)
        {
            _clientSettings = clientSettings;
            _client = null;
            return this;
        }

        public IElasticsearchProviderConfigurator Client(Uri uri)
        {
            _clientSettings = new ElasticsearchClientSettings(uri);
            _client = null;
            return this;
        }
        
        public IElasticsearchProviderConfigurator Id(Func<Core.AuditEvent, Id> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }

        public IElasticsearchProviderConfigurator Index(string indexName)
        {
            _index = (IndexName)indexName;
            return this;
        }

        public IElasticsearchProviderConfigurator Index(Func<Core.AuditEvent, IndexName> indexNameBuilder)
        {
            _index = indexNameBuilder;
            return this;
        }

    }
}
