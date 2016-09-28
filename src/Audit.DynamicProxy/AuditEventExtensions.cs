using Audit.Core;

namespace Audit.DynamicProxy
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Dynamic Interception Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditInterceptEvent GetAuditInterceptEvent(this AuditEvent auditEvent)
        {
            return auditEvent.CustomFields.ContainsKey("InterceptEvent")
                ? auditEvent.CustomFields["InterceptEvent"] as AuditInterceptEvent
                : null;
        }
    }
}
