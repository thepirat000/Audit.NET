using Audit.Core;

namespace Audit.Http
{
    public class AuditEventHttpClient : AuditEvent
    {
        /// <summary>
        /// Gets or sets the HttpClient event details.
        /// </summary>
        public HttpAction Action { get; set; }
    }
}
