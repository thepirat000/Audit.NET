using System;
using Nest;
using Elastic = Elasticsearch.Net;
using static Nest.ConnectionSettings;

namespace Audit.Elasticsearch.Providers
{
    /// <summary>
    /// Elasticsearch connection settings class with proper serialization for audit events
    /// </summary>
    public class AuditConnectionSettings : ConnectionSettingsBase<AuditConnectionSettings>
    {
        public AuditConnectionSettings(Uri uri = null)
            : this(new Elastic.SingleNodeConnectionPool(uri ?? new Uri("http://localhost:9200"), null))
        {
        }
        public AuditConnectionSettings(Elastic.InMemoryConnection connection)
            : this(new Elastic.SingleNodeConnectionPool(new Uri("http://localhost:9200"), null), connection)
        {
        }

        public AuditConnectionSettings(Elastic.IConnectionPool connectionPool)
            : this(connectionPool, null, null)
        {
        }

        public AuditConnectionSettings(Elastic.IConnectionPool connectionPool, Elastic.IConnection connection)
            : this(connectionPool, connection, null)
        {
        }

        protected AuditConnectionSettings(Elastic.IConnectionPool connectionPool, Elastic.IConnection connection, SourceSerializerFactory sourceSerializerFactory, IPropertyMappingProvider propertyMappingProvider) 
            : base(connectionPool, connection, sourceSerializerFactory, propertyMappingProvider)
        {
        }

        protected AuditConnectionSettings(Elastic.IConnectionPool connectionPool, Elastic.IConnection connection, IPropertyMappingProvider propertyMappingProvider)
            : base(connectionPool, connection, (builtin, settings) => new AuditJsonNetSerializer(builtin, settings) , propertyMappingProvider)
        {
        }
    }
}
