using Audit.Core;

using Hangfire.Server;

using System;

namespace Audit.Hangfire.ConfigurationApi;

public class AuditHangfireJobExecutionConfigurator : IAuditHangfireJobExecutionConfigurator
{
    public AuditJobExecutionOptions Options { get; } = new AuditJobExecutionOptions();

    public IAuditHangfireJobExecutionConfigurator AuditWhen(Func<PerformContext, bool> jobFilter)
    {
        Options.AuditWhen = jobFilter;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator ExcludeArguments(Func<PerformContext, bool> excludeArguments)
    {
        Options.ExcludeArguments = excludeArguments;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator ExcludeArguments(bool excludeArguments = true)
    {
        Options.ExcludeArguments = _ => excludeArguments;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator EventType(Func<PerformContext, string> eventType)
    {
        Options.EventType = eventType;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator EventType(string eventTypeTemplate)
    {
        Options.EventType = _ => eventTypeTemplate;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator DataProvider(Func<PerformContext, IAuditDataProvider> dataProvider)
    {
        Options.DataProvider = dataProvider;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator DataProvider(IAuditDataProvider dataProvider)
    {
        Options.DataProvider = _ => dataProvider;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy)
    {
        Options.EventCreationPolicy = eventCreationPolicy;
        return this;
    }

    public IAuditHangfireJobExecutionConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
    {
        Options.AuditScopeFactory = auditScopeFactory;
        return this;
    }
}