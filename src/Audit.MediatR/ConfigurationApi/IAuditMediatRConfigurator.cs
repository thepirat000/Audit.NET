using System;
using Audit.Core;

namespace Audit.MediatR.ConfigurationApi;

public interface IAuditMediatRConfigurator
{
    AuditMediatROptions Options { get; }
    IAuditMediatRConfigurator CallFilter(Func<MediatRCallContext, bool> callFilter);
    IAuditMediatRConfigurator IncludeRequest(Func<MediatRCallContext, bool> includeRequest);
    IAuditMediatRConfigurator IncludeRequest(bool includeRequest = true);
    IAuditMediatRConfigurator IncludeResponse(Func<MediatRCallContext, bool> includeResponse);
    IAuditMediatRConfigurator IncludeResponse(bool includeResponse = true);
    IAuditMediatRConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy);
    IAuditMediatRConfigurator DataProvider(Func<MediatRCallContext, IAuditDataProvider> dataProvider);
    IAuditMediatRConfigurator DataProvider(IAuditDataProvider dataProvider);
    IAuditMediatRConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);
}