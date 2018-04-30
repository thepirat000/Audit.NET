using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Elastic = Elasticsearch.Net;

namespace Audit.Elasticsearch.Providers
{
    /// <summary>
    /// Json serializer implementation for Audit Events, using the Audit.NET's global JsonSettings.
    /// </summary>
    public class AuditJsonNetSerializer : JsonNetSerializer
    {
        public AuditJsonNetSerializer(Elastic.IElasticsearchSerializer builtinSerializer, IConnectionSettingsValues connectionSettings) 
            : base(builtinSerializer, connectionSettings)
        {
        }
        protected override JsonSerializerSettings CreateJsonSerializerSettings()
        {
            return Audit.Core.Configuration.JsonSettings;
        }
    }
}
