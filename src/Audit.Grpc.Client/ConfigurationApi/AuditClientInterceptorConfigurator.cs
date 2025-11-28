using System;
using Audit.Core;

namespace Audit.Grpc.Client.ConfigurationApi;

public class AuditClientInterceptorConfigurator : IAuditClientInterceptorConfigurator
{
    internal Func<CallContext, bool> _callFilter;
    internal Func<CallContext, bool> _includeRequestHeaders;
    internal Func<CallContext, bool> _includeResponseHeaders;
    internal Func<CallContext, bool> _includeTrailers;
    internal Func<CallContext, bool> _includeRequest;
    internal Func<CallContext, bool> _includeResponse;
    internal Func<CallContext, string> _eventTypeName;
    internal EventCreationPolicy? _eventCreationPolicy;
    internal Func<CallContext, IAuditDataProvider> _auditDataProvider;
    internal IAuditScopeFactory _auditScopeFactory;

    public IAuditClientInterceptorConfigurator CallFilter(Func<CallContext, bool> callPredicate)
    {
        _callFilter = callPredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeRequestHeaders(bool include = true)
    {
        _includeRequestHeaders = _ => include;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeRequestHeaders(Func<CallContext, bool> includePredicate)
    {
        _includeRequestHeaders = includePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeResponseHeaders(bool include = true)
    {
        _includeResponseHeaders = _ => include;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeResponseHeaders(Func<CallContext, bool> includePredicate)
    {
        _includeResponseHeaders = includePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeTrailers(bool include = true)
    {
        _includeTrailers = _ => include;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeTrailers(Func<CallContext, bool> includePredicate)
    {
        _includeTrailers = includePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeRequestPayload(bool include = true)
    {
        _includeRequest = _ => include;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeRequestPayload(Func<CallContext, bool> includePredicate)
    {
        _includeRequest = includePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeResponsePayload(bool include = true)
    {
        _includeResponse = _ => include;
        return this;
    }

    public IAuditClientInterceptorConfigurator IncludeResponsePayload(Func<CallContext, bool> includePredicate)
    {
        _includeResponse = includePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator EventType(string eventTypeName)
    {
        _eventTypeName = _ => eventTypeName;
        return this;
    }

    public IAuditClientInterceptorConfigurator EventType(Func<CallContext, string> eventTypeNamePredicate)
    {
        _eventTypeName = eventTypeNamePredicate;
        return this;
    }

    public IAuditClientInterceptorConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy)
    {
        _eventCreationPolicy = eventCreationPolicy;
        return this;
    }

    public IAuditClientInterceptorConfigurator AuditDataProvider(IAuditDataProvider auditDataProvider)
    {
        _auditDataProvider = _ => auditDataProvider;
        return this;
    }

    public IAuditClientInterceptorConfigurator AuditDataProvider(Func<CallContext, IAuditDataProvider> auditDataProviderPredicate)
    {
        _auditDataProvider = auditDataProviderPredicate;
        return this;
    }


    public IAuditClientInterceptorConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
    {
        _auditScopeFactory = auditScopeFactory;
        return this;
    }
}