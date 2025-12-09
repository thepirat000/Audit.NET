using Hangfire.Client;
using Hangfire.Server;

namespace Audit.Hangfire;

public static class HangfireContextExtensions
{
    /// <summary>
    /// Gets the Hangfire Job Creation Event from the CreateContext.
    /// </summary>
    public static AuditEventHangfireJobCreation GetAuditEvent(this CreateContext context)
    {
        if (context.Items.TryGetValue(AuditJobCreationFilterAttribute.AuditEventKey, out var auditEventObject) 
            && auditEventObject is AuditEventHangfireJobCreation auditEvent)
        {
            return auditEvent;
        }

        return null;
    }

    /// <summary>
    /// Gets the Hangfire Job Execution Event from the PerformContext.
    /// </summary>
    public static AuditEventHangfireJobExecution GetAuditEvent(this PerformContext context)
    {
        if (context.Items.TryGetValue(AuditJobExecutionFilterAttribute.AuditEventKey, out var auditEventObject)
            && auditEventObject is AuditEventHangfireJobExecution auditEvent)
        {
            return auditEvent;
        }

        return null;
    }
}