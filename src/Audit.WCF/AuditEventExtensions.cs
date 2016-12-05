using Audit.Core;

namespace Audit.WCF
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the WCF Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static WcfEvent GetWcfAuditAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventWcfAction)
            {
                return (auditEvent as AuditEventWcfAction).WcfEvent;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("WcfEvent") 
                ? auditEvent.CustomFields["WcfEvent"] as WcfEvent
                : null;
        }
    }
}
