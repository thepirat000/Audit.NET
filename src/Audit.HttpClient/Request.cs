using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.Http
{
    public class Request
    {
        public string QueryString { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Headers { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Content Content { get; set; }
    }
}
