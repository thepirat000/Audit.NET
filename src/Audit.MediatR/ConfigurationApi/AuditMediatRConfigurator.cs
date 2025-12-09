using System;
using Audit.Core;

namespace Audit.MediatR.ConfigurationApi;

public class AuditMediatRConfigurator : IAuditMediatRConfigurator
{
    public AuditMediatROptions Options { get; } = new AuditMediatROptions();


    public IAuditMediatRConfigurator CallFilter(Func<MediatRCallContext, bool> callFilter)
    {
        Options.CallFilter = callFilter;

        return this;
    }
    
    public IAuditMediatRConfigurator IncludeRequest(Func<MediatRCallContext, bool> includeRequest)
    {
        Options.IncludeRequest = includeRequest;

        return this;
    }

    public IAuditMediatRConfigurator IncludeRequest(bool includeRequest = true)
    {
        Options.IncludeRequest = _ => includeRequest;

        return this;
    }

    public IAuditMediatRConfigurator IncludeResponse(Func<MediatRCallContext, bool> includeResponse)
    {
        Options.IncludeResponse = includeResponse;
        return this;
    }

    public IAuditMediatRConfigurator IncludeResponse(bool includeResponse = true)
    {
        Options.IncludeResponse = _ => includeResponse;
        return this;
    }

    public IAuditMediatRConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy)
    {
        Options.EventCreationPolicy = eventCreationPolicy;
        return this;
    }

    public IAuditMediatRConfigurator DataProvider(Func<MediatRCallContext, IAuditDataProvider> dataProvider)
    {
        Options.DataProvider = dataProvider;
        return this;
    }

    public IAuditMediatRConfigurator DataProvider(IAuditDataProvider dataProvider)
    {
        Options.DataProvider = _ => dataProvider;
        return this;
    }

    public IAuditMediatRConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
    {
        Options.AuditScopeFactory = auditScopeFactory;
        return this;
    }

    public IAuditMediatRConfigurator EventType(Func<MediatRCallContext, string> eventType)
    {
        Options.EventType = eventType; 
        return this;
    }

    public IAuditMediatRConfigurator EventType(string eventTypeTemplate)
    {
        Options.EventType = _ => eventTypeTemplate;
        return this;
    }
}