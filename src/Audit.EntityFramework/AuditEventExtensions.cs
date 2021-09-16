using Audit.Core;

namespace Audit.EntityFramework
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Entity Framework Event portion of the Audit Event on the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static EntityFrameworkEvent GetEntityFrameworkEvent(this IAuditScope auditScope)
        {
            return auditScope?.Event.GetEntityFrameworkEvent();
        }

        /// <summary>
        /// Gets the Entity Framework Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static EntityFrameworkEvent GetEntityFrameworkEvent(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventEntityFramework)
            {
                return (auditEvent as AuditEventEntityFramework).EntityFrameworkEvent;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("EntityFrameworkEvent")
                ? Core.Configuration.JsonAdapter.ToObject<EntityFrameworkEvent>(auditEvent.CustomFields["EntityFrameworkEvent"])
                : null;
        }

#if EF_CORE_3 || EF_CORE_5 || EF_CORE_6
        /// <summary>
        /// Gets the Low-Level EF Command Event portion of the Audit Event on the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static CommandEvent GetCommandEntityFrameworkEvent(this IAuditScope auditScope)
        {
            return auditScope?.Event.GetCommandEntityFrameworkEvent();
        }

        /// <summary>
        /// Gets the Low-Level EF Command Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static CommandEvent GetCommandEntityFrameworkEvent(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventCommandEntityFramework)
            {
                return (auditEvent as AuditEventCommandEntityFramework).CommandEvent;
            }
            return auditEvent.CustomFields.ContainsKey("CommandEvent")
                ? Core.Configuration.JsonAdapter.ToObject<CommandEvent>(auditEvent.CustomFields["CommandEvent"])
                : null;
        }
#endif
    }
}
