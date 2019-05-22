using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.Http
{
    public class Content
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Body { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Headers { get; set; }
    }
}
