using Audit.Core;

using Hangfire.Server;

using System;
using Hangfire.Common;

namespace Audit.Hangfire.ConfigurationApi;

/// <summary>
/// Options that control how Audit.NET captures Hangfire job execution.
/// All delegates are evaluated per job execution and are used by <see cref="Audit.Hangfire.AuditJobExecutionFilterAttribute"/>.
/// </summary>
/// <remarks>
/// Precedence: when using the attribute, delegates defined here take precedence over the attribute's simple properties.
/// </remarks>
public class AuditJobExecutionOptions
{
    /// <summary>
    /// Predicate that determines whether the job execution should be audited.
    /// Return true to enable auditing for the given <see cref="PerformContext"/>; false to skip. Default is to audit all jobs.
    /// </summary>
    public Func<PerformContext, bool> AuditWhen { get; set; }

    /// <summary>
    /// Delegate that returns the event type string to set in the <see cref="AuditEvent.EventType"/> field.
    /// Supports placeholders: {type} (job type name) and {method} (method name).
    /// If null, the attribute value or the default "{type}.{method}" is used.
    /// </summary>
    public Func<PerformContext, string> EventType { get; set; }

    /// <summary>
    /// Delegate that determines whether to exclude the job arguments (<see cref="Job.Args"/>) from the audit event.
    /// Return true to exclude; false to include. Default is false (include arguments).
    /// </summary>
    public Func<PerformContext, bool> ExcludeArguments { get; set; }

    /// <summary>
    /// Delegate that resolves the <see cref="IAuditDataProvider"/> to use for this job execution event.
    /// If null, falls back to the provider resolved by the audit scope factory / global configuration.
    /// </summary>
    public Func<PerformContext, IAuditDataProvider> DataProvider { get; set; }

    /// <summary>
    /// Event creation policy to use for the audit scope. When null, the global setting is used.
    /// </summary>
    public EventCreationPolicy? EventCreationPolicy { get; set; }

    /// <summary>
    /// Overrides the <see cref="IAuditScopeFactory"/> to create scopes for this event. When null, the global factory is used.
    /// </summary>
    public IAuditScopeFactory AuditScopeFactory { get; set; }
}