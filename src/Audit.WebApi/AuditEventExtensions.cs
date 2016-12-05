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
            if (auditEvent is AuditEventWebApi)
            {
                return (auditEvent as AuditEventWebApi).Action;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("Action")
                ? auditEvent.CustomFields["Action"] as AuditApiAction
                : null;
        }
    }
}
