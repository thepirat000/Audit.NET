using Audit.Core;

namespace Audit.Grpc.Server
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the Server Call Event portion of the Audit Event on the given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static GrpcServerCallAction GetServerCallAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetServerCallAction();
        }

        /// <summary>
        /// Gets the Server Call Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static GrpcServerCallAction GetServerCallAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventGrpcServer server)
            {
                return server.Action;
            }
            
            return null;
        }
    }
}
