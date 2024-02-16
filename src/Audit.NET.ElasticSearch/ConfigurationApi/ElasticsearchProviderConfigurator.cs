using System;
using Audit.Core;
using Elasticsearch.Net;
using Nest;

namespace Audit.Elasticsearch.Configuration
{
    public class ElasticsearchProviderConfigurator : IElasticsearchProviderConfigurator
    {
        internal IConnectionSettingsValues _connectionSettings;
        internal Setting<IndexName> _index;
        internal Func<Core.AuditEvent, Id> _idBuilder;

        public IElasticsearchProviderConfigurator ConnectionSettings(IConnectionSettingsValues connectionSettings)
        {
            _connectionSettings = connectionSettings;
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(Uri uri)
        {
            _connectionSettings = new ConnectionSettings(uri);
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(IConnectionPool connectionPool)
        {
            _connectionSettings = new ConnectionSettings(connectionPool);
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(IConnectionPool connectionPool, IConnection connection)
        {
            _connectionSettings = new ConnectionSettings(connectionPool, connection);
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
