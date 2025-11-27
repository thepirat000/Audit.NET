using System;
using Audit.Core;
using Audit.Core.Extensions;

using Grpc.Core;
using Grpc.Core.Interceptors;

using System.Threading.Tasks;

namespace Audit.Grpc.Server;

public class AuditServerInterceptor : Interceptor
{
    /// <summary>
    /// The key used to store the Audit Event in the Server Call Context UserState.
    /// </summary>
    public const string AuditEventKey = "__AuditEvent";

    /// <summary>
    /// Sets a filter function to determine the gRPC call events to audit depending on the Call Context. By default, all calls are audited.
    /// </summary>
    public Func<ServerCallContext, bool> CallFilter { get; set; }

    /// <summary>
    /// A predicate to determine whether request headers should be included on the audit output. By default, request headers are not included.
    /// </summary>
    public Func<ServerCallContext, bool> IncludeRequestHeaders { get; set; }

    /// <summary>
    /// A predicate to determine whether response trailers should be included on the audit output. By default, response trailers are not included.
    /// </summary>
    public Func<ServerCallContext, bool> IncludeTrailers { get; set; }

    /// <summary>
    /// A predicate to determine whether the request message should be included on the audit output. By default, the request message is not included.
    /// </summary>
    public Func<ServerCallContext, bool> IncludeRequestPayload { get; set; }

    /// <summary>
    /// A predicate to determine whether the response message should be included on the audit output. By default, the response message is not included.
    /// </summary>
    public Func<ServerCallContext, bool> IncludeResponsePayload { get; set; }

    /// <summary>
    /// A function to determine the event type name to use in the audit output. The following placeholders can be used as part of the string:
    /// - {service}: replaced with the service name.
    /// - {method}: replaced with the method name.
    /// By default, the event type is "/{service}/{method}".
    /// </summary>
    public Func<ServerCallContext, string> EventTypeName { get; set; }

    /// <summary>
    /// The event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
    /// </summary>
    public EventCreationPolicy? EventCreationPolicy { get; set; }

    /// <summary>
    /// The audit data provider to use. Default is NULL to use the globally configured data provider.
    /// </summary>
    public IAuditDataProvider DataProvider { get; set; }

    /// <summary>
    /// The Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
    /// </summary>
    public IAuditScopeFactory AuditScopeFactory { get; set; }

    public AuditServerInterceptor()
    {

    }

    public AuditServerInterceptor(Action<ConfigurationApi.IAuditServerInterceptorConfigurator> config)
    {
        var interceptorConfig = new ConfigurationApi.AuditServerInterceptorConfigurator();
        if (config != null)
        {
            config.Invoke(interceptorConfig);

            CallFilter = interceptorConfig._callFilter;
            IncludeRequestHeaders = interceptorConfig._includeRequestHeaders;
            IncludeTrailers = interceptorConfig._includeTrailers;
            IncludeRequestPayload = interceptorConfig._includeRequest;
            IncludeResponsePayload = interceptorConfig._includeResponse;
            EventTypeName = interceptorConfig._eventTypeName;
            EventCreationPolicy = interceptorConfig._eventCreationPolicy;
            DataProvider = interceptorConfig._auditDataProvider;
            AuditScopeFactory = interceptorConfig._auditScopeFactory;
        }
    }

    // Unary call interception
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, 
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return await base.UnaryServerHandler(request, context, continuation);
        }

        var auditEvent = CreateGrpcServerAuditEvent(request, context, nameof(MethodType.Unary));

        var action = auditEvent.Action;

        await using var auditScope = await CreateAuditScopeAsync(context, auditEvent);

        try
        {
            var response = await base.UnaryServerHandler(request, context, continuation);

            if (IncludeResponsePayload?.Invoke(context) == true)
            {
                action.Response = response;
            }

            action.IsSuccess = true;

            return response;
        }
        catch (RpcException rpcEx)
        {
            action.IsSuccess = false;
            action.Exception = rpcEx.GetExceptionInfo();
            action.StatusCode = rpcEx.StatusCode.ToString();
            action.StatusDetail = rpcEx.Status.Detail;

            throw;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();

            throw;
        }
        finally
        {
            HydrateResponseTrailers(action, context.ResponseTrailers, context);
        }
    }

    // Client streaming call interception
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return await base.ClientStreamingServerHandler(requestStream, context, continuation);
        }

        var auditEvent = CreateGrpcServerAuditEvent<TRequest>(null, context, nameof(MethodType.ClientStreaming));

        await using var auditScope = await CreateAuditScopeAsync(context, auditEvent);

        var includeRequest = IncludeRequestPayload?.Invoke(context) == true;

        var action = auditEvent.Action;

        var stream = includeRequest ? new ClientStreamReaderWrapper<TRequest>(requestStream, action) : requestStream;
        
        try
        {
            var response = await base.ClientStreamingServerHandler(stream, context, continuation);

            if (IncludeResponsePayload?.Invoke(context) == true)
            {
                action.Response = response;
            }

            action.IsSuccess = true;

            return response;
        }
        catch (RpcException rpcEx)
        {
            action.IsSuccess = false;
            action.Exception = rpcEx.GetExceptionInfo();
            action.StatusCode = rpcEx.StatusCode.ToString();
            action.StatusDetail = rpcEx.Status.Detail;

            throw;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();
            throw;
        }
        finally
        {
            HydrateResponseTrailers(action, context.ResponseTrailers, context);
        }
    }

    // Server streaming call interception
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            await base.ServerStreamingServerHandler(request, responseStream, context, continuation);

            return;
        }

        var auditEvent = CreateGrpcServerAuditEvent(request, context, nameof(MethodType.ServerStreaming));

        await using var auditScope = await CreateAuditScopeAsync(context, auditEvent);

        var action = auditEvent.Action;

        var stream = IncludeResponsePayload?.Invoke(context) == true
            ? new ServerStreamWriterWrapper<TResponse>(responseStream, action)
            : responseStream;
        
        try
        {
            await base.ServerStreamingServerHandler(request, stream, context, continuation);

            action.IsSuccess = true;
        }
        catch (RpcException rpcEx)
        {
            action.IsSuccess = false;
            action.Exception = rpcEx.GetExceptionInfo();
            action.StatusCode = rpcEx.StatusCode.ToString();
            action.StatusDetail = rpcEx.Status.Detail;

            throw;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();
            throw;
        }
        finally
        {
            HydrateResponseTrailers(action, context.ResponseTrailers, context);
        }
    }

    // Duplex streaming call interception
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            return;
        }

        var auditEvent = CreateGrpcServerAuditEvent<TRequest>(null, context, nameof(MethodType.DuplexStreaming));

        await using var auditScope = await CreateAuditScopeAsync(context, auditEvent);

        var includeRequest = IncludeRequestPayload?.Invoke(context) == true;

        var action = auditEvent.Action;

        var wrappedRequestStream = includeRequest 
            ? new ClientStreamReaderWrapper<TRequest>(requestStream, action) 
            : requestStream;
        
        var wrappedResponseStream = IncludeResponsePayload?.Invoke(context) == true
            ? new ServerStreamWriterWrapper<TResponse>(responseStream, action)
            : responseStream;

        try
        {
            await base.DuplexStreamingServerHandler(wrappedRequestStream, wrappedResponseStream, context, continuation);
            
            action.IsSuccess = true;
        }
        catch (RpcException rpcEx)
        {
            action.IsSuccess = false;
            action.Exception = rpcEx.GetExceptionInfo();
            action.StatusCode = rpcEx.StatusCode.ToString();
            action.StatusDetail = rpcEx.Status.Detail;

            throw;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();
            throw;
        }
        finally
        {
            HydrateResponseTrailers(action, context.ResponseTrailers, context);
        }
    }

    private void HydrateResponseTrailers(GrpcServerCallAction call, Metadata responseTrailers, ServerCallContext context)
    {
        if (responseTrailers != null && IncludeTrailers?.Invoke(context) == true)
        {
            call.Trailers = [];

            foreach (var trailer in responseTrailers)
            {
                call.Trailers.Add(new GrpcMetadata()
                {
                    Key = trailer.Key,
                    IsBinary = trailer.IsBinary,
                    Value = !trailer.IsBinary ? trailer.Value : null,
                    ValueBytes = trailer.IsBinary ? trailer.ValueBytes : null
                });
            }
        }
    }

    protected virtual Task<IAuditScope> CreateAuditScopeAsync(ServerCallContext context, AuditEvent auditEvent)
    {
        var auditScopeFactory = AuditScopeFactory ?? Configuration.AuditScopeFactory;

        var auditScope = auditScopeFactory.CreateAsync(new AuditScopeOptions
        {
            AuditEvent = auditEvent,
            CreationPolicy = EventCreationPolicy,
            DataProvider = DataProvider
        });

        return auditScope;
    }

    protected internal virtual bool IsAuditDisabled(ServerCallContext context)
    {
        if (Configuration.AuditDisabled)
        {
            return true;
        }

        if (CallFilter == null)
        {
            return false;
        }

        return !CallFilter.Invoke(context);
    }

    private AuditEventGrpcServer CreateGrpcServerAuditEvent<TRequest>(TRequest request, ServerCallContext context, string methodType) 
        where TRequest : class 
    {
        var action = new GrpcServerCallAction
        {
            MethodType = methodType,
            MethodName = context.Method,
            Deadline = context.Deadline,
            Peer = context.Peer,
            Host = context.Host,

            ServerCallContext = context
        };
        
        if (IncludeRequestHeaders?.Invoke(context) == true)
        {
            action.RequestHeaders = [];

            foreach (var header in context.RequestHeaders)
            {
                action.RequestHeaders.Add(new GrpcMetadata()
                {
                    Key = header.Key,
                    IsBinary = header.IsBinary,
                    Value = !header.IsBinary ? header.Value : null,
                    ValueBytes = header.IsBinary ? header.ValueBytes : null
                });
            }
        }

        if (request != null && IncludeRequestPayload?.Invoke(context) == true)
        {
            action.Request = request;
        }

        var eventType = (EventTypeName?.Invoke(context) ?? "{method}")
            .Replace("{method}", context.Method);

        var auditEvent = new AuditEventGrpcServer()
        {
            Action = action,
            EventType = eventType
        };

        context.UserState[AuditEventKey] = auditEvent;

        return auditEvent;
    }
}
