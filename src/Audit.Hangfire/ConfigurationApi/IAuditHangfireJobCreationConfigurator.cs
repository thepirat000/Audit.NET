using Audit.Core;

using Hangfire.Client;
using Hangfire.Common;

using System;
using System.Collections.Generic;

namespace Audit.Hangfire.ConfigurationApi
{
    /// <summary>
    /// Fluent configurator for <see cref="AuditJobCreationOptions"/>.
    /// Used to configure how Audit.NET captures Hangfire job creation.
    /// </summary>
    public interface IAuditHangfireJobCreationConfigurator
    {
        /// <summary>
        /// The options being configured for job creation auditing.
        /// </summary>
        AuditJobCreationOptions Options { get; }

        /// <summary>
        /// Sets a predicate to determine if a job creation should be audited.
        /// Return true to audit the job in the given <see cref="CreateContext"/>; false to skip. Default is to audit all jobs.
        /// </summary>
        /// <param name="jobFilter">Predicate evaluated per job creation.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator AuditWhen(Func<CreateContext, bool> jobFilter);

        /// <summary>
        /// Sets a delegate indicating whether to include <see cref="CreateContext.Parameters"/> in the audit event.
        /// Return true to include; false to exclude. Defaults to false (exclude parameters).
        /// </summary>
        /// <param name="includeParameters">Delegate evaluated per job creation.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator IncludeParameters(Func<CreateContext, bool> includeParameters);

        /// <summary>
        /// Indicates whether to include <see cref="CreateContext.Parameters"/> in the audit event. Defaults to false (exclude parameters)
        /// </summary>
        /// <param name="includeParameters">True to include parameters; false to exclude.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator IncludeParameters(bool includeParameters = true);

        /// <summary>
        /// Sets a delegate indicating whether to exclude job arguments (<see cref="Job.Args"/>) from the audit event. Defaults to false (include arguments).
        /// Return true to exclude; false to include.
        /// </summary>
        /// <param name="excludeArguments">Delegate evaluated per job creation.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator ExcludeArguments(Func<CreateContext, bool> excludeArguments);

        /// <summary>
        /// Indicates whether to exclude job arguments (<see cref="Job.Args"/>) from the audit event. Defaults to false (include arguments).
        /// </summary>
        /// <param name="excludeArguments">True to exclude arguments; false to include.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator ExcludeArguments(bool excludeArguments = true);

        /// <summary>
        /// Sets a delegate that returns the event type name template for the audit event.
        /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
        /// </summary>
        /// <param name="eventType">Delegate returning the event type string.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator EventType(Func<CreateContext, string> eventType);

        /// <summary>
        /// Sets the event type name template used for the audit event.
        /// Supports placeholders: {type} (job type name) and {method} (method name). Defaults to "{type}.{method}".
        /// </summary>
        /// <param name="eventTypeTemplate">Event type template, e.g. "{type}.{method}".</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator EventType(string eventTypeTemplate);

        /// <summary>
        /// Sets a delegate that resolves the <see cref="IAuditDataProvider"/> per job creation.
        /// </summary>
        /// <param name="dataProvider">Delegate returning the data provider.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator DataProvider(Func<CreateContext, IAuditDataProvider> dataProvider);

        /// <summary>
        /// Sets a fixed <see cref="IAuditDataProvider"/> for job creation events.
        /// </summary>
        /// <param name="dataProvider">The data provider instance.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator DataProvider(IAuditDataProvider dataProvider);

        /// <summary>
        /// Sets the event creation policy to use for audit scopes during job creation.
        /// </summary>
        /// <param name="eventCreationPolicy">The event creation policy.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy);

        /// <summary>
        /// Sets the <see cref="IAuditScopeFactory"/> used to create audit scopes for job creation.
        /// </summary>
        /// <param name="auditScopeFactory">The audit scope factory.</param>
        /// <returns>This configurator instance.</returns>
        IAuditHangfireJobCreationConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);

        /// <summary>
        /// Sets a delegate that returns additional custom fields to include in the audit job creation event.
        /// </summary>
        /// <param name="customFields">Delegate returning a dictionary of custom fields.</param>
        IAuditHangfireJobCreationConfigurator WithCustomFields(Func<CreateContext, Dictionary<string, object>> customFields);
    }
}
