using Audit.Core;

namespace Audit.Wcf.Client
{
    public class AuditEventWcfClient : AuditEvent
    {
        /// <summary>
        /// Gets or sets the WCF action details.
        /// </summary>
        public WcfClientAction WcfClientEvent { get; set; }
    }
}