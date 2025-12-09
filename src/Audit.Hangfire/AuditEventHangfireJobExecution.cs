using Audit.Core;

namespace Audit.Hangfire;

/// <summary>
/// Audit Event for Hangfire Job Execution.
/// </summary>
public class AuditEventHangfireJobExecution : AuditEvent
{
    /// <summary>
    /// Hangfire Job Execution Event details.
    /// </summary>

    public HangfireJobExecutionEvent JobExecution { get; set; }
}