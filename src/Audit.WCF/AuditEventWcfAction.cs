using Audit.Core;

namespace Audit.WCF
{
    /// <summary>
    /// Represents the output of the audit process for a WCF action
    /// </summary>
    public class AuditEventWcfAction : AuditEvent
    {
        /// <summary>
        /// Gets or sets the WCF action details.
        /// </summary>
        public WcfEvent WcfEvent { get; set; }
    }
}
