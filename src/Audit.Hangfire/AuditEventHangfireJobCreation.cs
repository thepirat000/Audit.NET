using Audit.Core;

namespace Audit.Hangfire;

/// <summary>
/// Audit Event for Hangfire Job Creation.
/// </summary>
public class AuditEventHangfireJobCreation : AuditEvent
{
    /// <summary>
    /// Hangfire Job Creation Event details.
    /// </summary>
    public HangfireJobCreationEvent JobCreation { get; set; }
}