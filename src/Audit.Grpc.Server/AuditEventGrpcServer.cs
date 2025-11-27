using Audit.Core;

namespace Audit.Grpc.Server;

/// <summary>
/// Audit event for gRPC server calls.
/// </summary>
public class AuditEventGrpcServer : AuditEvent
{
    /// <summary>
    /// The gRPC server call details.
    /// </summary>
    public GrpcServerCallAction Action { get; set; }
}