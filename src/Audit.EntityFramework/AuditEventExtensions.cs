using Audit.Core;

namespace Audit.EntityFramework
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Entity Framework Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static EntityFrameworkEvent GetEntityFrameworkEvent(this AuditEvent auditEvent)
        {
            return auditEvent.CustomFields.ContainsKey("EntityFrameworkEvent")
                ? auditEvent.CustomFields["EntityFrameworkEvent"] as EntityFrameworkEvent
                : null;
        }
    }
}
