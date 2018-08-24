using Audit.Core;

namespace Audit.DynamicProxy
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Dynamic Interception Event portion of the Audit Event for the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static InterceptEvent GetAuditInterceptEvent(this AuditScope auditScope)
        {
            return auditScope?.Event.GetAuditInterceptEvent();
        }

        /// <summary>
        /// Gets the Dynamic Interception Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static InterceptEvent GetAuditInterceptEvent(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventIntercept)
            {
                return (auditEvent as AuditEventIntercept).InterceptEvent;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("InterceptEvent")
                ? auditEvent.CustomFields["InterceptEvent"] as InterceptEvent
                : null;
        }
    }
}
