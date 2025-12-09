using Audit.Core;

namespace Audit.Hangfire;

public static class AuditEventExtensions
{
    /// <summary>
    /// Gets the Hangfire Job Creation Event portion of the Audit Event on the given scope.
    /// </summary>
    /// <param name="auditScope">The audit scope.</param>
    public static HangfireJobCreationEvent GetHangfireJobCreationEvent(this AuditScope auditScope)
    {
        return auditScope?.Event.GetHangfireJobCreationEvent();
    }

    /// <summary>
    /// Gets the Hangfire Job Creation portion of the Audit Event.
    /// </summary>
    /// <param name="auditEvent">The audit event.</param>
    public static HangfireJobCreationEvent GetHangfireJobCreationEvent(this AuditEvent auditEvent)
    {
        if (auditEvent is AuditEventHangfireJobCreation ev)
        {
            return ev.JobCreation;
        }

        return null;
    }

    /// <summary>
    /// Gets the Hangfire Job Execution Event portion of the Audit Event on the given scope.
    /// </summary>
    /// <param name="auditScope">The audit scope.</param>
    public static HangfireJobExecutionEvent GetHangfireJobExecutionEvent(this AuditScope auditScope)
    {
        return auditScope?.Event.GetHangfireJobExecutionEvent();
    }

    /// <summary>
    /// Gets the Hangfire Job Execution portion of the Audit Event.
    /// </summary>
    /// <param name="auditEvent">The audit event.</param>
    public static HangfireJobExecutionEvent GetHangfireJobExecutionEvent(this AuditEvent auditEvent)
    {
        if (auditEvent is AuditEventHangfireJobExecution ev)
        {
            return ev.JobExecution;
        }

        return null;
    }
}