using Audit.Core;
using Newtonsoft.Json;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents the output of the audit process for a SignalR event
    /// </summary>
    public class AuditEventSignalr : AuditEvent
    {
        /// <summary>
        /// Gets or sets the SignalR event details.
        /// </summary>
        [JsonProperty(Order = 10)]
        public SignalrEventBase Event { get; set; }
    }
}
