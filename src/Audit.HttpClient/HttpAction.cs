using Audit.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.Http
{
    public class HttpAction : IAuditOutput
    {
        public string Method { get; set; }
        public string Url { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
        public Request Request { get; set; }
        public Response Response { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes this HttpAction as a JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Core.Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an HttpAction from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Audit Action representation.</param>
        public static HttpAction FromJson(string json)
        {
            return JsonConvert.DeserializeObject<HttpAction>(json, Core.Configuration.JsonSettings);
        }

    }
}
