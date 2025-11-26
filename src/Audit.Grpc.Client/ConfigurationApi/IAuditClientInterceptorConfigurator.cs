using Audit.Core;

using System;

namespace Audit.Grpc.Client.ConfigurationApi;

public interface IAuditClientInterceptorConfigurator
{
    /// <summary>
    /// Specifies a filter function to determine which calls to audit. By default, all calls are audited.
    /// </summary>
    IAuditClientInterceptorConfigurator CallFilter(Func<CallContext, bool> callPredicate);

    /// <summary>
    /// Specifies whether request headers should be included on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeRequestHeaders(bool include = true);
    /// <summary>
    /// Specifies a predicate to determine whether request headers should be included.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeRequestHeaders(Func<CallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether response headers should be included on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeResponseHeaders(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether response headers should be included.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeResponseHeaders(Func<CallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether response trailers should be included on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeTrailers(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether response trailers should be included.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeTrailers(Func<CallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether the request message should be included on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeRequestPayload(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether the request message should be included.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeRequestPayload(Func<CallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether the response message should be included on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeResponsePayload(bool include = true);
    /// <summary>
    /// Specifies a predicate to determine whether the response message should be included.
    /// </summary>
    IAuditClientInterceptorConfigurator IncludeResponsePayload(Func<CallContext, bool> includePredicate);

    /// <summary>
    /// Specifies a predicate to determine the event type name on the audit output.
    /// </summary>
    IAuditClientInterceptorConfigurator EventType(Func<CallContext, string> eventTypeNamePredicate);

    /// <summary>
    /// Specifies the event type name to use.
    /// </summary>
    IAuditClientInterceptorConfigurator EventType(string eventTypeName);

    /// <summary>
    /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
    /// </summary>
    IAuditClientInterceptorConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy);

    /// <summary>
    /// Specifies the audit data provider to use. Default is NULL to use the globally configured data provider.
    /// </summary>
    IAuditClientInterceptorConfigurator AuditDataProvider(IAuditDataProvider auditDataProvider);

    /// <summary>
    /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
    /// </summary>
    IAuditClientInterceptorConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);
}