using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Audit.Http
{
    public class Request
    {
        public string QueryString { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Content Content { get; set; }
        public Dictionary<string, object> Options { get; set; }
    }
}
