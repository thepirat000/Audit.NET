using Audit.Core;
using Newtonsoft.Json;

namespace Audit.Http
{
    public class AuditEventHttpClient : AuditEvent
    {
        /// <summary>
        /// Gets or sets the HttpClient event details.
        /// </summary>
        [JsonProperty(Order = 10)]
        public HttpAction Action { get; set; }
    }
}
