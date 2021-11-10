using Audit.Core;

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
        public SignalrEventBase Event { get; set; }
    }
}
