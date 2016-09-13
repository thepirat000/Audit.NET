using Audit.Core;

namespace Audit.Mvc
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the MVC Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditAction GetMvcAuditAction(this AuditEvent auditEvent)
        {
            return auditEvent.CustomFields.ContainsKey("Action") 
                ? auditEvent.CustomFields["Action"] as AuditAction
                : null;
        }
    }
}
