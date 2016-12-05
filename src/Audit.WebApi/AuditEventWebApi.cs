using Audit.Core;
using Newtonsoft.Json;

namespace Audit.WebApi
{
    /// <summary>
    /// Represents the output of the audit process for a Web API action
    /// </summary>
    public class AuditEventWebApi : AuditEvent
    {
        /// <summary>
        /// Gets or sets the Web API action details.
        /// </summary>
        [JsonProperty(Order = 10)]
        public AuditApiAction Action { get; set; }
    }
}

