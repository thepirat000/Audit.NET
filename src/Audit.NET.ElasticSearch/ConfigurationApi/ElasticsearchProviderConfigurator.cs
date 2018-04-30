using System;
using Audit.Elasticsearch.Providers;
using Elasticsearch.Net;
using Nest;

namespace Audit.Elasticsearch.Configuration
{
    public class ElasticsearchProviderConfigurator : IElasticsearchProviderConfigurator
    {
        internal IConnectionSettingsValues _connectionSettings;
        internal Func<Core.AuditEvent, IndexName> _indexBuilder;
        internal Func<Core.AuditEvent, TypeName> _typeNameBuilder;
        internal Func<Core.AuditEvent, Id> _idBuilder;

        public IElasticsearchProviderConfigurator ConnectionSettings(AuditConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(Uri uri)
        {
            _connectionSettings = new AuditConnectionSettings(uri);
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(IConnectionPool connectionPool)
        {
            _connectionSettings = new AuditConnectionSettings(connectionPool);
            return this;
        }

        public IElasticsearchProviderConfigurator ConnectionSettings(IConnectionPool connectionPool, IConnection connection)
        {
            _connectionSettings = new AuditConnectionSettings(connectionPool, connection);
            return this;
        }

        public IElasticsearchProviderConfigurator Id(Func<Core.AuditEvent, Id> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }

        public IElasticsearchProviderConfigurator Index(string indexName)
        {
            _indexBuilder = ev => indexName;
            return this;
        }

        public IElasticsearchProviderConfigurator Index(Func<Core.AuditEvent, IndexName> indexNameBuilder)
        {
            _indexBuilder = indexNameBuilder;
            return this;
        }

        public IElasticsearchProviderConfigurator Type(Func<Core.AuditEvent, TypeName> typeNameBuilder)
        {
            _typeNameBuilder = typeNameBuilder;
            return this;
        }

        public IElasticsearchProviderConfigurator Type(string typeName)
        {
            _typeNameBuilder = ev => typeName;
            return this;
        }
    }
}
