using Audit.Core;
using Audit.Core.Extensions;
using Audit.Hangfire.ConfigurationApi;

using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

using System;
using System.Linq;

namespace Audit.Hangfire;

/// <summary>
/// Hangfire Client filter that audits Hangfire job creation via Audit.NET.
/// Captures job metadata and creates an <see cref="AuditEventHangfireJobCreation"/> within an <see cref="IAuditScope"/>.
/// </summary>
/// <remarks>
/// Options delegates take precedence over the simple properties on this attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
public class AuditJobCreationFilterAttribute : JobFilterAttribute, IClientFilter
{
    /// <summary>
    /// Context items key used to store the job creation start time (<see cref="DateTime"/>).
    /// </summary>
    public const string StartedAtKey = "__AuditJobCreationFilter:StartedAt";

    /// <summary>
    /// Context items key used to store the generated <see cref="AuditEvent"/>.
    /// </summary>
    public const string AuditEventKey = "__AuditJobCreationFilter:AuditEvent";

    /// <summary>
    /// Options to configure auditing behavior for job creation.
    /// Delegates defined here override the simple properties on this attribute.
    /// </summary>
    public AuditJobCreationOptions Options { get; set; } = new();

    /// <summary>
    /// Template used for the audit event type.
    /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// Whether to include <see cref="CreatingContext.Parameters"/> in the audit event. Defaults to false.
    /// </summary>
    public bool IncludeParameters { get; set; }

    /// <summary>
    /// Whether to exclude <see cref="Job.Args"/> from the audit event. Defaults to false.
    /// </summary>
    public bool ExcludeArguments { get; set; }

    /// <summary>
    /// Creates an instance with default options.
    /// </summary>
    public AuditJobCreationFilterAttribute()
    {
    }

    /// <summary>
    /// Creates an instance using the provided options.
    /// </summary>
    /// <param name="options">Auditing options for job creation.</param>
    public AuditJobCreationFilterAttribute(AuditJobCreationOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// Creates an instance using a fluent configurator to build options.
    /// </summary>
    /// <param name="configurator">Action to configure auditing options.</param>
    public AuditJobCreationFilterAttribute(Action<IAuditHangfireJobCreationConfigurator> configurator)
    {
        var config = new AuditHangfireJobCreationConfigurator();
        if (configurator != null)
        {
            configurator.Invoke(config);

            Options = config.Options;
        }
    }

    /// <inheritdoc/>
    public void OnCreating(CreatingContext context)
    {
        var createdAt = Configuration.SystemClock.GetCurrentDateTime();

        if (IsAuditDisabled(context))
        {
            return;
        }

        // Store the start time in the context items
        context.Items[StartedAtKey] = createdAt;
    }

    /// <inheritdoc/>
    public void OnCreated(CreatedContext context)
    {
        if (IsAuditDisabled(context))
        {
            return;
        }

        var jobCreation = new HangfireJobCreationEvent()
        {
            JobId = context.BackgroundJob.Id,
            CreatedAt = context.BackgroundJob.CreatedAt,
            Exception = context.Exception.GetExceptionInfo(),
            InitialState = context.InitialState?.Name,
            Args = AreArgumentsExcluded(context) ? null : context.BackgroundJob.Job?.Args?.ToList(),
            Parameters = AreParametersIncluded(context) ? context.Parameters?.ToDictionary(k => k.Key, v => v.Value) : null,
            TypeName = context.BackgroundJob.Job?.Type?.GetFullTypeName(),
            MethodName = context.BackgroundJob.Job?.Method?.ToString(),
            Canceled = context.Canceled,
            Context = context
        };

        if (context.InitialState is ScheduledState scheduled)
        {
            // Scheduled job
            jobCreation.ScheduledAt = scheduled.ScheduledAt;
            jobCreation.EnqueueAt = scheduled.EnqueueAt;
        }
        else if (context.InitialState is EnqueuedState enqueued)
        {
            // Fire-and-forget
            jobCreation.EnqueuedAt = enqueued.EnqueuedAt;
            jobCreation.Queue = enqueued.Queue;
        }
        else if (context.InitialState is AwaitingState awaiting)
        {
            // Continuation job
            jobCreation.Continuation = new ContinuationData()
            {
                ParentId = awaiting.ParentId,
                Option = awaiting.Options.ToString(),
                Expiration = awaiting.Expiration
            };
        }

        jobCreation.CustomFields = Options.CustomFields?.Invoke(context);

        var auditEvent = new AuditEventHangfireJobCreation
        {
            JobCreation = jobCreation
        };

        // Store the audit event in the context
        context.Items[AuditEventKey] = auditEvent;

        using var auditScope = CreateAuditScope(auditEvent, context);
    }

    internal bool AreArgumentsExcluded(CreateContext context)
    {
        if (Options.ExcludeArguments != null)
        {
            return Options.ExcludeArguments.Invoke(context);
        }

        return ExcludeArguments;
    }

    internal bool AreParametersIncluded(CreateContext context)
    {
        if (Options.IncludeParameters != null)
        {
            return Options.IncludeParameters.Invoke(context);
        }

        return IncludeParameters;
    }

    protected internal IAuditScope CreateAuditScope(AuditEvent auditEvent, CreateContext context)
    {
        var auditScopeFactory = Options.AuditScopeFactory ?? Configuration.AuditScopeFactory;

        var eventType = (Options.EventType?.Invoke(context) ?? EventType ?? "{type}.{method}")
            .Replace("{type}", context.Job.Type.Name)
            .Replace("{method}", context.Job.Method.Name);

        var auditScope = auditScopeFactory.Create(new AuditScopeOptions
        {
            AuditEvent = auditEvent,
            EventType = eventType,
            CreationPolicy = Options.EventCreationPolicy,
            DataProvider = Options.DataProvider?.Invoke(context)
        });

        if (context.Items.TryGetValue(StartedAtKey, out var startedAtObject) && startedAtObject is DateTime startedAt)
        {
            auditEvent.StartDate = startedAt;
            context.Items.Remove(StartedAtKey);
        }
        
        return auditScope;
    }

    protected internal bool IsAuditDisabled(CreateContext context)
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

}