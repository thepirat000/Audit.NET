using Audit.Core;

namespace Audit.WebApi
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Web API Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditApiAction GetWebApiAuditAction(this AuditEvent auditEvent)
        {
            return auditEvent.CustomFields["Action"] as AuditApiAction;
        }
    }
}
