using Audit.Core;

using System;
using Grpc.Core;

namespace Audit.Grpc.Server.ConfigurationApi;

public interface IAuditServerInterceptorConfigurator
{
    /// <summary>
    /// Specifies a filter function to determine which calls to audit. By default, all calls are audited.
    /// </summary>
    IAuditServerInterceptorConfigurator CallFilter(Func<ServerCallContext, bool> callPredicate);

    /// <summary>
    /// Specifies whether request headers should be included on the audit output.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeRequestHeaders(bool include = true);
    /// <summary>
    /// Specifies a predicate to determine whether request headers should be included.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeRequestHeaders(Func<ServerCallContext, bool> includePredicate);
    
    /// <summary>
    /// Specifies whether response trailers should be included on the audit output.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeTrailers(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether response trailers should be included.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeTrailers(Func<ServerCallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether the request message should be included on the audit output.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeRequestPayload(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether the request message should be included.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeRequestPayload(Func<ServerCallContext, bool> includePredicate);

    /// <summary>
    /// Specifies whether the response message should be included on the audit output.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeResponsePayload(bool include = true);

    /// <summary>
    /// Specifies a predicate to determine whether the response message should be included.
    /// </summary>
    IAuditServerInterceptorConfigurator IncludeResponsePayload(Func<ServerCallContext, bool> includePredicate);

    /// <summary>
    /// Specifies a predicate to determine the event type name on the audit output.
    /// </summary>
    IAuditServerInterceptorConfigurator EventType(Func<ServerCallContext, string> eventTypeNamePredicate);

    /// <summary>
    /// Specifies the event type name to use.
    /// </summary>
    IAuditServerInterceptorConfigurator EventType(string eventTypeName);

    /// <summary>
    /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
    /// </summary>
    IAuditServerInterceptorConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy);

    /// <summary>
    /// Specifies the audit data provider instance to use for audit scopes created by this interceptor.
    /// </summary>
    /// <param name="auditDataProvider"> The concrete <see cref="IAuditDataProvider"/> instance to use.</param>
    /// <remarks>
    /// By default, it will use the globally configured data provider in Audit.Core.Configuration.DataProvider.
    /// </remarks>
    IAuditServerInterceptorConfigurator AuditDataProvider(IAuditDataProvider auditDataProvider);

    /// <summary>
    /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
    /// </summary>
    /// <remarks>
    /// By default, it will use the globally configured scope factory in Audit.Core.Configuration.AuditScopeFactory.
    /// </remarks>
    IAuditServerInterceptorConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);
}