using Audit.Core;
using System.Collections.Generic;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.Http
{
    public class HttpAction : IAuditOutput
    {
        public string Method { get; set; }

        public string Url { get; set; }

#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
	    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public string Version { get; set; }

        public Request Request { get; set; }

        public Response Response { get; set; }

#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
	    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public string Exception { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes this HttpAction as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an HttpAction from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Audit Action representation.</param>
        public static HttpAction FromJson(string json)
        {
            return Configuration.JsonAdapter.Deserialize<HttpAction>(json);
        }

    }
}
