using Audit.Core;

namespace Audit.Grpc.Client;

/// <summary>
/// Audit event for gRPC client calls.
/// </summary>
public class AuditEventGrpcClient : AuditEvent
{
    /// <summary>
    /// The gRPC client call details.
    /// </summary>
    public GrpcClientCallAction Action { get; set; }
}