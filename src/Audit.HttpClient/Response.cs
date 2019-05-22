using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.Http
{
    public class Response
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public bool IsSuccess { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Headers { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Content Content { get; set; }
    }
}
