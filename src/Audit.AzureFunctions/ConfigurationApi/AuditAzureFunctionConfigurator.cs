using System;
using System.Collections.Generic;
using Audit.Core;
using Microsoft.Azure.Functions.Worker;

namespace Audit.AzureFunctions.ConfigurationApi;

public class AuditAzureFunctionConfigurator : IAuditAzureFunctionConfigurator
{
    public AuditAzureFunctionOptions Options { get; } = new AuditAzureFunctionOptions();

    public IAuditAzureFunctionConfigurator AuditWhen(Func<FunctionContext, bool> callFilter)
    {
        Options.AuditWhen = callFilter;
        return this;
    }

    public IAuditAzureFunctionConfigurator IncludeFunctionDefinition(Func<FunctionContext, bool> includePredicate)
    {
        Options.IncludeFunctionDefinition = includePredicate;
        return this;
    }

    public IAuditAzureFunctionConfigurator IncludeFunctionDefinition(bool include = true)
    {
        Options.IncludeFunctionDefinition = _ => include;
        return this;
    }

    public IAuditAzureFunctionConfigurator IncludeTriggerInfo(Func<FunctionContext, bool> includePredicate)
    {
        Options.IncludeTriggerInfo = includePredicate;
        return this;
    }

    public IAuditAzureFunctionConfigurator IncludeTriggerInfo(bool include = true)
    {
        Options.IncludeTriggerInfo = _ => include;
        return this;
    }

    public IAuditAzureFunctionConfigurator DataProvider(Func<FunctionContext, IAuditDataProvider> dataProvider)
    {
        Options.DataProvider = dataProvider;
        return this;
    }

    public IAuditAzureFunctionConfigurator DataProvider(IAuditDataProvider dataProvider)
    {
        Options.DataProvider = _ => dataProvider;
        return this;
    }

    public IAuditAzureFunctionConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy)
    {
        Options.EventCreationPolicy = eventCreationPolicy;
        return this;
    }

    public IAuditAzureFunctionConfigurator WithCustomFields(Func<FunctionContext, Dictionary<string, object>> customFields)
    {
        Options.CustomFields = customFields;
        return this;
    }
    
    public IAuditAzureFunctionConfigurator EventType(Func<FunctionContext, string> eventType)
    {
        Options.EventType = eventType;
        return this;
    }
    public IAuditAzureFunctionConfigurator EventType(string eventType)
    {
        Options.EventType = _ => eventType;
        return this;
    }
}