using Audit.Core;

namespace Audit.Mvc
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the MVC Event portion of the Audit Event for the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static AuditAction GetMvcAuditAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetMvcAuditAction();
        }

        /// <summary>
        /// Gets the MVC Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static AuditAction GetMvcAuditAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventMvcAction action)
            {
                return action.Action;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.TryGetValue("Action", out var field)
                ? Configuration.JsonAdapter.ToObject<AuditAction>(field)
                : null;
        }
    }
}
