using System;
using Audit.Core;
using Grpc.Core;

namespace Audit.Grpc.Server.ConfigurationApi;

public class AuditServerInterceptorConfigurator : IAuditServerInterceptorConfigurator
{
    internal Func<ServerCallContext, bool> _callFilter;
    internal Func<ServerCallContext, bool> _includeRequestHeaders;
    internal Func<ServerCallContext, bool> _includeTrailers;
    internal Func<ServerCallContext, bool> _includeRequest;
    internal Func<ServerCallContext, bool> _includeResponse;
    internal Func<ServerCallContext, string> _eventTypeName;
    internal EventCreationPolicy? _eventCreationPolicy;
    internal Func<ServerCallContext, IAuditDataProvider> _auditDataProvider;
    internal IAuditScopeFactory _auditScopeFactory;

    public IAuditServerInterceptorConfigurator CallFilter(Func<ServerCallContext, bool> callPredicate)
    {
        _callFilter = callPredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeRequestHeaders(bool include = true)
    {
        _includeRequestHeaders = _ => include;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeRequestHeaders(Func<ServerCallContext, bool> includePredicate)
    {
        _includeRequestHeaders = includePredicate;
        return this;
    }
    
    public IAuditServerInterceptorConfigurator IncludeTrailers(bool include = true)
    {
        _includeTrailers = _ => include;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeTrailers(Func<ServerCallContext, bool> includePredicate)
    {
        _includeTrailers = includePredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeRequestPayload(bool include = true)
    {
        _includeRequest = _ => include;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeRequestPayload(Func<ServerCallContext, bool> includePredicate)
    {
        _includeRequest = includePredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeResponsePayload(bool include = true)
    {
        _includeResponse = _ => include;
        return this;
    }

    public IAuditServerInterceptorConfigurator IncludeResponsePayload(Func<ServerCallContext, bool> includePredicate)
    {
        _includeResponse = includePredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator EventType(string eventTypeName)
    {
        _eventTypeName = _ => eventTypeName;
        return this;
    }

    public IAuditServerInterceptorConfigurator EventType(Func<ServerCallContext, string> eventTypeNamePredicate)
    {
        _eventTypeName = eventTypeNamePredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy)
    {
        _eventCreationPolicy = eventCreationPolicy;
        return this;
    }

    public IAuditServerInterceptorConfigurator AuditDataProvider(IAuditDataProvider auditDataProvider)
    {
        _auditDataProvider = _ => auditDataProvider;
        return this;
    }

    public IAuditServerInterceptorConfigurator AuditDataProvider(Func<ServerCallContext, IAuditDataProvider> auditDataProviderPredicate)
    {
        _auditDataProvider = auditDataProviderPredicate;
        return this;
    }

    public IAuditServerInterceptorConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
    {
        _auditScopeFactory = auditScopeFactory;
        return this;
    }
}