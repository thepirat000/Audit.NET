using Audit.Core;

namespace Audit.WebApi
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Web API Event portion of the Audit Event for a given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static AuditApiAction GetWebApiAuditAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetWebApiAuditAction();
        }

        /// <summary>
        /// Gets the Web API Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditApiAction GetWebApiAuditAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventWebApi api)
            {
                return api.Action;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("Action")
                ? Configuration.JsonAdapter.ToObject<AuditApiAction>(auditEvent.CustomFields["Action"])
                : null;
        }
    }
}
