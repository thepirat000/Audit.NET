using Audit.Core;
using Newtonsoft.Json;

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
        [JsonProperty(Order = 10)]
        public WcfEvent WcfEvent { get; set; }
    }
}
