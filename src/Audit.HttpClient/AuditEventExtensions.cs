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
            if (auditEvent is AuditEventHttpClient client)
            {
                return client.Action;
            }
            // For backwards compatibility
            return auditEvent.CustomFields.ContainsKey("Action")
                ? Configuration.JsonAdapter.ToObject<HttpAction>(auditEvent.CustomFields["Action"])
                : null;
        }
    }
}
