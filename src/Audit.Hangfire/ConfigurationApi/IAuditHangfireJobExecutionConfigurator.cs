using Audit.Core;

using Hangfire.Common;
using Hangfire.Server;

using System;

namespace Audit.Hangfire.ConfigurationApi;

/// <summary>
/// Fluent configurator for <see cref="AuditJobExecutionOptions"/>.
/// Used to configure how Audit.NET captures Hangfire job execution.
/// </summary>
public interface IAuditHangfireJobExecutionConfigurator
{
    /// <summary>
    /// The options being configured for job execution auditing.
    /// </summary>
    AuditJobExecutionOptions Options { get; }

    /// <summary>
    /// Sets a predicate to determine if a job execution should be audited.
    /// Return true to audit the job in the given <see cref="PerformContext"/>; false to skip. Default is to audit all jobs.
    /// </summary>
    /// <param name="jobFilter">Predicate evaluated per job execution.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator AuditWhen(Func<PerformContext, bool> jobFilter);

    /// <summary>
    /// Sets a delegate indicating whether to exclude job arguments (<see cref="Job.Args"/>) from the audit event. Defaults to false (include arguments).
    /// Return true to exclude; false to include.
    /// </summary>
    /// <param name="excludeArguments">Delegate evaluated per job execution.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator ExcludeArguments(Func<PerformContext, bool> excludeArguments);

    /// <summary>
    /// Indicates whether to exclude job arguments (<see cref="Job.Args"/>) from the audit event. Defaults to false (include arguments).
    /// </summary>
    /// <param name="excludeArguments">True to exclude arguments; false to include.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator ExcludeArguments(bool excludeArguments = true);

    /// <summary>
    /// Sets a delegate that returns the event type name template for the audit event.
    /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
    /// </summary>
    /// <param name="eventType">Delegate returning the event type string.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator EventType(Func<PerformContext, string> eventType);

    /// <summary>
    /// Sets the event type name template used for the audit event.
    /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
    /// </summary>
    /// <param name="eventTypeTemplate">Event type template, e.g. "{type}.{method}".</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator EventType(string eventTypeTemplate);

    /// <summary>
    /// Sets a delegate that resolves the <see cref="IAuditDataProvider"/> per job execution.
    /// </summary>
    /// <param name="dataProvider">Delegate returning the data provider.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator DataProvider(Func<PerformContext, IAuditDataProvider> dataProvider);

    /// <summary>
    /// Sets a fixed <see cref="IAuditDataProvider"/> for job execution events.
    /// </summary>
    /// <param name="dataProvider">The data provider instance.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator DataProvider(IAuditDataProvider dataProvider);

    /// <summary>
    /// Sets the event creation policy to use for audit scopes during job execution.
    /// </summary>
    /// <param name="eventCreationPolicy">The event creation policy.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy);

    /// <summary>
    /// Sets the <see cref="IAuditScopeFactory"/> used to create audit scopes for job execution.
    /// </summary>
    /// <param name="auditScopeFactory">The audit scope factory.</param>
    /// <returns>This configurator instance.</returns>
    IAuditHangfireJobExecutionConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);

}