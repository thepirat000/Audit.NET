using Audit.Core;

namespace Audit.MediatR;

public static class AuditEventExtensions
{
    /// <summary>
    /// Gets the MediatR Call Event portion of the Audit Event on the given scope.
    /// </summary>
    /// <param name="auditScope">The audit scope.</param>
    public static MediatRCallAction GetMediatRCallAction(this AuditScope auditScope)
    {
        return auditScope?.Event.GetMediatRCallAction();
    }

    /// <summary>
    /// Gets the MediatR Call Event portion of the Audit Event.
    /// </summary>
    /// <param name="auditEvent">The audit event.</param>
    public static MediatRCallAction GetMediatRCallAction(this AuditEvent auditEvent)
    {
        if (auditEvent is AuditEventMediatR ev)
        {
            return ev.Call;
        }

        return null;
    }
}