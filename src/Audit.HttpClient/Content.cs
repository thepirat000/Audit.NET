using System.Collections.Generic;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.Http
{
    public class Content
    {
#if IS_NK_JSON
	    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public object Body { get; set; }

#if IS_NK_JSON
	    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public Dictionary<string, string> Headers { get; set; }
    }
}
