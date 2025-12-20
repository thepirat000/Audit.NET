using Audit.Core;

using Microsoft.Azure.Functions.Worker;

using System;
using System.Collections.Generic;

namespace Audit.AzureFunctions.ConfigurationApi;

/// <summary>
/// Configuration options for auditing Azure Functions executions.
/// </summary>
public class AuditAzureFunctionOptions
{
    /// <summary>
    /// Predicate that determines whether the call should be audited.
    /// Return true to enable auditing for the given <see cref="FunctionContext"/>; false to skip.
    /// Default is to audit all calls.
    /// </summary>
    public Func<FunctionContext, bool> AuditWhen { get; set; }

    /// <summary>
    /// Gets or sets a delegate that determines whether a function definition should be included based on the specified function context.
    /// </summary>
    /// <remarks>The delegate receives a <see cref="FunctionContext"/> instance and returns whether to include the function definition, or not.</remarks>
    public Func<FunctionContext, bool> IncludeFunctionDefinition { get; set; }

    /// <summary>
    /// Gets or sets a delegate that determines whether trigger information should be included for a given function context.
    /// </summary>
    /// <remarks>The delegate receives a <see cref="FunctionContext"/> and returns whether to include trigger information, or not.</remarks>
    public Func<FunctionContext, bool> IncludeTriggerInfo { get; set; }

    /// <summary>
    /// Delegate that returns the event type string to set in the <see cref="AuditEvent.EventType"/> field.
    /// Supports placeholders: {name} (function name), {id} (function id).
    /// By default, "{name}" is used.
    /// </summary>
    public Func<FunctionContext, string> EventType { get; set; }

    /// <summary>
    /// Delegate that resolves the <see cref="IAuditDataProvider"/> to use for this job execution event.
    /// If null, falls back to the provider resolved by the audit scope factory / global configuration.
    /// </summary>
    public Func<FunctionContext, IAuditDataProvider> DataProvider { get; set; }
        
    /// <summary>
    /// Event creation policy to use for the audit scope. When null, the global setting is used.
    /// </summary>
    public EventCreationPolicy? EventCreationPolicy { get; set; }

    /// <summary>
    /// Delegate that returns additional custom fields to include in the audit event.
    /// </summary>
    public Func<FunctionContext, Dictionary<string, object>> CustomFields { get; set; }

    /// <summary>
    /// Initializes default options.
    /// </summary>
    public AuditAzureFunctionOptions() { }

    /// <summary>
    /// Initializes options using the provided fluent configurator.
    /// </summary>
    /// <param name="configure">Configurator action to set up azure function auditing options.</param>
    public AuditAzureFunctionOptions(Action<IAuditAzureFunctionConfigurator> configure)
    {
        var configurator = new AuditAzureFunctionConfigurator();

        configure?.Invoke(configurator);
        
        var options = configurator.Options;
        
        IncludeFunctionDefinition = options.IncludeFunctionDefinition;
        IncludeTriggerInfo = options.IncludeTriggerInfo;
        EventType = options.EventType;
        DataProvider = options.DataProvider;
        EventCreationPolicy = options.EventCreationPolicy;
        CustomFields = options.CustomFields;
        AuditWhen = options.AuditWhen;
    }
}