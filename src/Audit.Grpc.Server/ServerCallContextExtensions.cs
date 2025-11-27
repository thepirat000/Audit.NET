using Audit.Grpc.Server;

namespace Grpc.Core;

public static class ServerCallContextExtensions
{
    /// <summary>
    /// Gets the Audit Event associated with the current Server Call Context.
    /// </summary>
    /// <param name="serverCallContext">The server call context.</param>
    public static AuditEventGrpcServer GetAuditEvent(this ServerCallContext serverCallContext)
    {
        if (serverCallContext.UserState.TryGetValue(AuditServerInterceptor.AuditEventKey, out var auditEvent) && auditEvent is AuditEventGrpcServer auditEventGrpcServer)
        {
            return auditEventGrpcServer;
        }

        return null;
    }

    /// <summary>
    /// Gets the Server Call Action associated with the current Server Call Context.
    /// </summary>
    /// <param name="serverCallContext">The server call context.</param>
    public static GrpcServerCallAction GetServerCallAction(this ServerCallContext serverCallContext)
    {
        return GetAuditEvent(serverCallContext)?.Action;
    }
}