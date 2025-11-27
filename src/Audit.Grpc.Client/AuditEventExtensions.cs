using Audit.Core;

namespace Audit.Grpc.Client
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Client Call Event portion of the Audit Event on the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static GrpcClientCallAction GetClientCallAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetClientCallAction();
        }

        /// <summary>
        /// Gets the Client Call Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static GrpcClientCallAction GetClientCallAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventGrpcClient client)
            {
                return client.Action;
            }
            
            return null;
        }
    }
}
