using Audit.Core;


namespace Audit.Http
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Entity Framework Event portion of the Audit Event on the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static HttpAction GetHttpAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetHttpAction();
        }

        /// <summary>
        /// Gets the Entity Framework Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static HttpAction GetHttpAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventHttpClient)
            {
                return (auditEvent as AuditEventHttpClient).Action;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.TryGetValue("Action", out var field)
                ? Configuration.JsonAdapter.ToObject<HttpAction>(field)
                : null;
        }
    }
}
