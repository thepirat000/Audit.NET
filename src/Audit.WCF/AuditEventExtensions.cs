using Audit.Core;

namespace Audit.WCF
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the WCF Event portion of the Audit Event for a given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static WcfEvent GetWcfAuditAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetWcfAuditAction();
        }

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
                ? Configuration.JsonAdapter.ToObject<WcfEvent>(auditEvent.CustomFields["WcfEvent"])
                : null;
        }
    }
}
