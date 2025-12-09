using System;
using Audit.Core;
using Audit.Core.Extensions;
using Audit.Hangfire.ConfigurationApi;

using Hangfire.Common;
using Hangfire.Server;

using System.Linq;

namespace Audit.Hangfire;

/// <summary>
/// Server filter that audits Hangfire job execution via Audit.NET.
/// Captures execution metadata and creates an <see cref="AuditEventHangfireJobExecution"/> within an <see cref="IAuditScope"/>.
/// </summary>
/// <remarks>
/// Delegates configured on <see cref="Options"/> take precedence over the simple properties exposed on this attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
public class AuditJobExecutionFilterAttribute : JobFilterAttribute, IServerFilter
{
    /// <summary>
    /// Context items key for the <see cref="IAuditScope"/> created on job start.
    /// </summary>
    public const string AuditScopeKey = "__AuditJobExecutionFilter:AuditScope";

    /// <summary>
    /// Context items key for the generated <see cref="AuditEventHangfireJobExecution"/>.
    /// </summary>
    public const string AuditEventKey = "__AuditJobExecutionFilter:AuditEvent";

    /// <summary>
    /// Options to configure auditing behavior for job execution.
    /// Delegates here override the simple properties on this attribute.
    /// </summary>
    public AuditJobExecutionOptions Options { get; set; } = new();

    /// <summary>
    /// Template for the audit event type when no delegate is provided via <see cref="AuditJobExecutionOptions.EventType"/>.
    /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// Whether to exclude <see cref="Job.Args"/> from the audit event. Defaults to false.
    /// </summary>
    public bool ExcludeArguments { get; set; }

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public AuditJobExecutionFilterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance using the provided options.
    /// </summary>
    /// <param name="options">Auditing options for job execution.</param>
    public AuditJobExecutionFilterAttribute(AuditJobExecutionOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// Initializes a new instance using a fluent configurator to build the options.
    /// </summary>
    /// <param name="configurator">Configurator action to set up auditing options.</param>
    public AuditJobExecutionFilterAttribute(Action<IAuditHangfireJobExecutionConfigurator> configurator)
    {
        var config = new AuditHangfireJobExecutionConfigurator();
        if (configurator != null)
        {
            configurator.Invoke(config);

            Options = config.Options;
        }
    }

    /// <inheritdoc/>
    public void OnPerforming(PerformingContext context)
    {
        if (IsAuditDisabled(context))
        {
            return;
        }

        var jobExecution = new HangfireJobExecutionEvent()
        {
            JobId = context.BackgroundJob.Id,
            CreatedAt = context.BackgroundJob.CreatedAt,
            ServerId = context.ServerId,
            Args = AreArgumentsExcluded(context) ? null : context.BackgroundJob.Job.Args?.ToList(),
            TypeName = context.BackgroundJob.Job?.Type?.GetFullTypeName(),
            MethodName = context.BackgroundJob.Job?.Method?.ToString(),
            Context = context
        };

        var auditEvent = new AuditEventHangfireJobExecution
        {
            JobExecution = jobExecution
        };

        // Store the audit event in the context
        context.Items[AuditEventKey] = auditEvent;

        var auditScope = CreateAuditScope(auditEvent, context);

        // Store the audit scope in the context for later use
        context.Items[AuditScopeKey] = auditScope;
    }

    /// <inheritdoc/>
    public void OnPerformed(PerformedContext context)
    {
        if (IsAuditDisabled(context))
        {
            return;
        }

        if (!context.Items.TryGetValue(AuditScopeKey, out var auditScopeObj) || !(auditScopeObj is IAuditScope auditScope))
        {
            return;
        }

        context.Items.Remove(AuditScopeKey);

        var jobExecution = auditScope.EventAs<AuditEventHangfireJobExecution>()?.JobExecution;

        if (jobExecution != null)
        {
            jobExecution.Result = context.Result;
            jobExecution.Exception = context.Exception.GetExceptionInfo();
            jobExecution.Canceled = context.Canceled;
        }

        auditScope.Dispose();
    }

    protected internal IAuditScope CreateAuditScope(AuditEvent auditEvent, PerformContext context)
    {
        var auditScopeFactory = Options.AuditScopeFactory ?? Configuration.AuditScopeFactory;

        var eventType = (Options.EventType?.Invoke(context) ?? EventType ?? "{type}.{method}")
            .Replace("{type}", context.BackgroundJob.Job?.Type?.Name)
            .Replace("{method}", context.BackgroundJob.Job?.Method?.Name);

        var auditScope = auditScopeFactory.Create(new AuditScopeOptions
        {
            AuditEvent = auditEvent,
            EventType = eventType,
            CreationPolicy = Options.EventCreationPolicy,
            DataProvider = Options.DataProvider?.Invoke(context)
        });
        
        return auditScope;
    }

    protected internal bool IsAuditDisabled(PerformContext context)
    {
        if (Configuration.AuditDisabled)
        {
            return true;
        }

        if (Options.AuditWhen == null)
        {
            return false;
        }

        return !Options.AuditWhen.Invoke(context);
    }

    internal bool AreArgumentsExcluded(PerformContext context)
    {
        if (Options.ExcludeArguments != null)
        {
            return Options.ExcludeArguments.Invoke(context);
        }

        return ExcludeArguments;
    }
}
