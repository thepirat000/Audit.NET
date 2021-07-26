using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Elastic = Elasticsearch.Net;

namespace Audit.Elasticsearch.Providers
{
    /// <summary>
    /// Json serializer implementation for Audit Events using Newtonsoft.Json.
    /// </summary>
    public class AuditJsonNetSerializer : JsonNetSerializer
    {
        public static JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public AuditJsonNetSerializer(Elastic.IElasticsearchSerializer builtinSerializer, IConnectionSettingsValues connectionSettings) 
            : base(builtinSerializer, connectionSettings)
        {
        }

        protected override JsonSerializerSettings CreateJsonSerializerSettings()
        {
            return Settings;
        }
    }
}
