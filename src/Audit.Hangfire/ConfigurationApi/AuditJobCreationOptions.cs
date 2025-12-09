using Audit.Core;

using Hangfire.Client;
using Hangfire.Common;

using System;
using System.Collections.Generic;

namespace Audit.Hangfire.ConfigurationApi;

/// <summary>
/// Options that control how Audit.NET captures Hangfire job creation.
/// All delegates are evaluated per job creation and are used by <see cref="Audit.Hangfire.AuditJobCreationFilterAttribute"/>.
/// </summary>
/// <remarks>
/// Precedence: when using the attribute, delegates defined here take precedence over the attribute's simple properties.
/// </remarks>
public class AuditJobCreationOptions
{
    /// <summary>
    /// Predicate that determines whether the job creation should be audited.
    /// Return true to enable auditing for the given <see cref="CreateContext"/>; false to skip.
    /// Default is to audit all jobs.
    /// </summary>
    public Func<CreateContext, bool> AuditWhen { get; set; }

    /// <summary>
    /// Delegate that returns additional custom fields to include in the audit job creation event.
    /// </summary>
    public Func<CreateContext, Dictionary<string, object>> CustomFields { get; set; }

    /// <summary>
    /// Delegate that returns the event type string used for the audit event.
    /// Supports placeholders: {type} (job type name) and {method} (method name).
    /// If null, the attribute value or the default "{type}.{method}" is used.
    /// </summary>
    public Func<CreateContext, string> EventType { get; set; }

    /// <summary>
    /// Delegate that determines whether to include <see cref="CreateContext.Parameters"/> in the audit event.
    /// Return true to include; false to exclude. Default is false (exclude parameters).
    /// </summary>
    public Func<CreateContext, bool> IncludeParameters { get; set; }

    /// <summary>
    /// Delegate that determines whether to exclude the job arguments (<see cref="Job.Args"/>) from the audit event.
    /// Return true to exclude; false to include. Default is false (include arguments).
    /// </summary>
    public Func<CreateContext, bool> ExcludeArguments { get; set; }

    /// <summary>
    /// Delegate that resolves the <see cref="IAuditDataProvider"/> to use for this job creation event.
    /// If null, falls back to the provider resolved by the audit scope factory / global configuration.
    /// </summary>
    public Func<CreateContext, IAuditDataProvider> DataProvider { get; set; }
    
    /// <summary>
    /// Event creation policy to use for the audit scope. When null, the global setting is used.
    /// </summary>
    public EventCreationPolicy? EventCreationPolicy { get; set; }

    /// <summary>
    /// Overrides the <see cref="IAuditScopeFactory"/> to create scopes for this event. When null, the global factory is used.
    /// </summary>
    public IAuditScopeFactory AuditScopeFactory { get; set; }

    /// <summary>
    /// Initializes default options.
    /// </summary>
    public AuditJobCreationOptions()
    {
    }

    /// <summary>
    /// Initializes options using the provided fluent configurator.
    /// </summary>
    /// <param name="configure">Configurator action to set up job creation auditing options.</param>
    public AuditJobCreationOptions(Action<IAuditHangfireJobCreationConfigurator> configure)
    {
        if (configure == null)
        {
            return;
        }

        var configurator = new AuditHangfireJobCreationConfigurator();
        configure.Invoke(configurator);

        var options = configurator.Options;

        AuditWhen = options.AuditWhen;
        EventType = options.EventType;
        IncludeParameters = options.IncludeParameters;
        ExcludeArguments = options.ExcludeArguments;
        DataProvider = options.DataProvider;
        EventCreationPolicy = options.EventCreationPolicy;
        AuditScopeFactory = options.AuditScopeFactory;
    }
}