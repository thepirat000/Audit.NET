using System.Collections.Generic;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.Http
{
    public class Request
    {
        public string QueryString { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }

#if IS_NK_JSON
	    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public Dictionary<string, string> Headers { get; set; }

#if IS_NK_JSON
	    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public Content Content { get; set; }
    }
}
