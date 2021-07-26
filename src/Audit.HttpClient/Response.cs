using System.Collections.Generic;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.Http
{
    public class Response
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public bool IsSuccess { get; set; }
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
