using Audit.Core;

namespace Audit.WCF
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the WCF Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditWcfEvent GetWcfAuditAction(this AuditEvent auditEvent)
        {
            return auditEvent.CustomFields.ContainsKey(Configuration.CustomFieldName) 
                ? auditEvent.CustomFields[Configuration.CustomFieldName] as AuditWcfEvent
                : null;
        }
    }
}
