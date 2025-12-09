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

    /// <summary>
    /// Delegate that returns the event type string to set in the <see cref="AuditEvent.EventType"/> field.
    /// Supports placeholders: {requestType} (request type name) and {responseType} (response type name).
    /// Default is "{requestType}:{responseType}".
    /// </summary>
    /// <param name="eventType">Delegate returning the event type string.</param>
    /// <returns>This configurator instance.</returns>
    IAuditMediatRConfigurator EventType(Func<MediatRCallContext, string> eventType);

    /// <summary>
    /// Event type string to set in the <see cref="AuditEvent.EventType"/> field.
    /// Supports placeholders: {requestType} (request type name) and {responseType} (response type name).
    /// Default is "{requestType}:{responseType}".
    /// </summary>
    /// <param name="eventTypeTemplate">Event type template, e.g. "{requestType}:{responseType}".</param>
    /// <returns>This configurator instance.</returns>
    IAuditMediatRConfigurator EventType(string eventTypeTemplate);
}