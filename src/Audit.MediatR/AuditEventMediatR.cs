using Audit.Core;

namespace Audit.MediatR;

/// <summary>
/// Audit event class for MediatR
/// </summary>
public class AuditEventMediatR : AuditEvent
{
    /// <summary>
    /// The MediatR call details
    /// </summary>
    public MediatRCallAction Call { get; set; }
}