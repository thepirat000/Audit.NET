using Audit.Core;
using Audit.Core.Extensions;

using Grpc.Core;
using Grpc.Core.Interceptors;

using System;
using System.Threading.Tasks;

// ReSharper disable AsyncVoidLambda

namespace Audit.Grpc.Client;

/// <summary>
/// gRPC Client Interceptor to enable auditing of gRPC client calls.
/// </summary>
public class AuditClientInterceptor : Interceptor
{
    /// <summary>
    /// Sets a filter function to determine the gRPC call events to audit depending on the Call Context. By default, all calls are audited.
    /// </summary>
    public Func<CallContext, bool> CallFilter { get; set; }

    /// <summary>
    /// A predicate to determine whether request headers should be included on the audit output. By default, request headers are not included.
    /// </summary>
    public Func<CallContext, bool> IncludeRequestHeaders { get; set; }

    /// <summary>
    /// A predicate to determine whether response headers should be included on the audit output. By default, response headers are not included.
    /// </summary>
    public Func<CallContext, bool> IncludeResponseHeaders { get; set; }

    /// <summary>
    /// A predicate to determine whether response trailers should be included on the audit output. By default, response trailers are not included.
    /// </summary>
    public Func<CallContext, bool> IncludeTrailers { get; set; }

    /// <summary>
    /// A predicate to determine whether the request message should be included on the audit output. By default, the request message is not included.
    /// </summary>
    public Func<CallContext, bool> IncludeRequestPayload { get; set; }

    /// <summary>
    /// A predicate to determine whether the response message should be included on the audit output. By default, the response message is not included.
    /// </summary>
    public Func<CallContext, bool> IncludeResponsePayload { get; set; }

    /// <summary>
    /// A function to determine the event type name to use in the audit output. The following placeholders can be used as part of the string:
    /// - {service}: replaced with the service name.
    /// - {method}: replaced with the method name.
    /// By default, the event type is "/{service}/{method}".
    /// </summary>
    public Func<CallContext, string> EventTypeName { get; set; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditClientInterceptor"/> class with the default configuration.
    /// </summary>
    public AuditClientInterceptor() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditClientInterceptor"/> class with a configuration action.
    /// </summary>
    /// <param name="config">An action to configure the interceptor.</param>
    public AuditClientInterceptor(Action<ConfigurationApi.IAuditClientInterceptorConfigurator> config)
    {
        var interceptorConfig = new ConfigurationApi.AuditClientInterceptorConfigurator();
        if (config != null)
        {
            config.Invoke(interceptorConfig);

            CallFilter = interceptorConfig._callFilter;
            IncludeRequestHeaders = interceptorConfig._includeRequestHeaders;
            IncludeResponseHeaders = interceptorConfig._includeResponseHeaders;
            IncludeTrailers = interceptorConfig._includeTrailers;
            IncludeRequestPayload = interceptorConfig._includeRequest;
            IncludeResponsePayload = interceptorConfig._includeResponse;
            EventTypeName = interceptorConfig._eventTypeName;
            EventCreationPolicy = interceptorConfig._eventCreationPolicy;
            DataProvider = interceptorConfig._auditDataProvider;
            AuditScopeFactory = interceptorConfig._auditScopeFactory;
        }
    }

    #region Unary Calls

    /// <summary>
    /// Blocking Unary Call Interception
    /// </summary>
    /// <remarks>
    /// The BlockingUnaryCall API in gRPC C# only returns the response message (TResponse), not a call object or metadata container.
    /// Because of that, the interceptor override BlockingUnaryCall(...) doesn't have access to response headers or trailing metadata.
    /// </remarks>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return base.BlockingUnaryCall(request, context, continuation);
        }

        var auditEvent = CreateGrpcClientAuditEvent(request, context);

        var action = auditEvent.Action;

        using var auditScope = CreateAuditScope(auditEvent);

        try
        {
            var response = base.BlockingUnaryCall(request, context, continuation);

            if (IncludeResponsePayload?.Invoke(CallContext.From(context)) == true)
            {
                action.Response = response;
            }

            action.IsSuccess = true;
            
            // Since we don't have access to the Status in BlockingUnaryCall, we assume success if no exception was thrown.
            action.StatusCode = nameof(StatusCode.OK);

            return response;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();
            throw;
        }
    }

    /// <summary>
    /// Asynchronous Unary Call Interception
    /// </summary>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return base.AsyncUnaryCall(request, context, continuation);
        }

        var auditEvent = CreateGrpcClientAuditEvent(request, context);

        var auditScopeTask = CreateAuditScopeAsync(auditEvent);

        var call = continuation(request, context);
        
        return new AsyncUnaryCall<TResponse>(
            HandleAsyncUnaryCallResponse(call.ResponseAsync, call.GetTrailers, call.GetStatus, auditScopeTask, context),
            HandleResponseHeaders(call.ResponseHeadersAsync, auditEvent, context),
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }
    
    private async Task<TResponse> HandleAsyncUnaryCallResponse<TRequest, TResponse>(Task<TResponse> responseTask,
        Func<Metadata> getTrailers,
        Func<Status> getStatus,
        Task<IAuditScope> auditScopeTask, ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        await using var auditScope = await auditScopeTask;

        var action = auditScope.EventAs<AuditEventGrpcClient>().Action;

        try
        {
            var response = await responseTask;

            if (IncludeResponsePayload?.Invoke(CallContext.From(context)) == true)
            {
                action.Response = response;
            }

            return response;
        }
        catch (Exception ex)
        {
            action.IsSuccess = false;
            action.Exception = ex.GetExceptionInfo();
            
            throw;
        }
        finally
        {
            HydrateResponseStatus(action, getStatus());

            HydrateResponseTrailers(action, getTrailers(), context);
        }
    }

    #endregion

    #region Streaming Calls

    /// <summary>
    /// Client Streaming Call Interception
    /// </summary>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return base.AsyncClientStreamingCall(context, continuation);
        }

        var auditEvent = CreateGrpcClientAuditEvent(null, context);

        var auditScopeTask = CreateAuditScopeAsync(auditEvent);

        var call = continuation(context);

        var stream = IncludeRequestPayload?.Invoke(CallContext.From(context)) == true
            ? new ClientStreamWriterWrapper<TRequest>(call.RequestStream, auditEvent.Action)
            : call.RequestStream;

        return new AsyncClientStreamingCall<TRequest, TResponse>(
            stream,
            HandleAsyncClientStreamingCallResponse(call.ResponseAsync, call.GetTrailers, call.GetStatus, auditScopeTask, context),
            HandleResponseHeaders(call.ResponseHeadersAsync, auditEvent, context),
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleAsyncClientStreamingCallResponse<TRequest, TResponse>(Task<TResponse> callResponseAsync,
        Func<Metadata> getTrailers,
        Func<Status> getStatus,
        Task<IAuditScope> auditScopeTask,
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class

    {
        await using var auditScope = await auditScopeTask;
        var action = auditScope.EventAs<AuditEventGrpcClient>().Action;

        try
        {
            var response = await callResponseAsync;

            if (IncludeResponsePayload?.Invoke(CallContext.From(context)) == true)
            {
                action.Response = response;
            }

            return response;
        }
        catch (Exception ex)
        {
            action.Exception = ex.GetExceptionInfo();
            action.IsSuccess = false;

            throw;
        }
        finally
        {
            HydrateResponseStatus(action, getStatus());

            HydrateResponseTrailers(action, getTrailers(), context);
        }
    }

    /// <summary>
    /// Server Streaming Call Interception
    /// </summary>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return base.AsyncServerStreamingCall(request, context, continuation);
        }

        var auditEvent = CreateGrpcClientAuditEvent(request, context);

        var auditScopeTask = CreateAuditScopeAsync(auditEvent);

        var call = continuation(request, context);

        var includeResponse = IncludeResponsePayload?.Invoke(CallContext.From(context)) == true;

        var wrappedRequestStream = new ServerStreamWriterWrapper<TResponse>(call.ResponseStream, auditScopeTask, includeResponse);

        return new AsyncServerStreamingCall<TResponse>(
            wrappedRequestStream,
            HandleResponseHeaders(call.ResponseHeadersAsync, auditEvent, context),
            call.GetStatus,
            call.GetTrailers,
            async () =>
            {
                var auditScope = await auditScopeTask;
                
                var action = auditScope.EventAs<AuditEventGrpcClient>().Action;
                
                HydrateResponseStatus(action, call.GetStatus());

                HydrateResponseTrailers(action, call.GetTrailers(), context);
                
                await auditScope.DisposeAsync();

                call.Dispose();
            });
    }

    /// <summary>
    /// Duplex Streaming Call Interception
    /// </summary>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        if (IsAuditDisabled(context))
        {
            return base.AsyncDuplexStreamingCall(context, continuation);
        }

        var auditEvent = CreateGrpcClientAuditEvent(null, context);
        var auditScopeTask = CreateAuditScopeAsync(auditEvent);

        var call = continuation(context);

        var requestStream = IncludeRequestPayload?.Invoke(CallContext.From(context)) == true
            ? new ClientStreamWriterWrapper<TRequest>(call.RequestStream, auditEvent.Action)
            : call.RequestStream;

        var includeResponse = IncludeResponsePayload?.Invoke(CallContext.From(context)) == true;

        var responseStream = new ServerStreamWriterWrapper<TResponse>(call.ResponseStream, auditScopeTask, includeResponse);
        
        return new AsyncDuplexStreamingCall<TRequest, TResponse>(
            requestStream,
            responseStream,
            HandleResponseHeaders(call.ResponseHeadersAsync, auditEvent, context),
            call.GetStatus,
            call.GetTrailers,
            async () =>
            {
                var auditScope = await auditScopeTask;

                var action = auditScope.EventAs<AuditEventGrpcClient>().Action;

                HydrateResponseStatus(action, call.GetStatus());

                HydrateResponseTrailers(action, call.GetTrailers(), context);

                await auditScope.DisposeAsync();

                call.Dispose();
            });
    }

    #endregion

    #region Helpers

    private async Task<Metadata> HandleResponseHeaders<TRequest, TResponse>(Task<Metadata> responseHeadersTask, 
        AuditEventGrpcClient auditEvent,
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var action = auditEvent.Action;

        var responseHeaders = await responseHeadersTask;

        HydrateResponseHeaders(action, responseHeaders, context);

        return responseHeaders;
    }

    private void HydrateResponseHeaders<TRequest, TResponse>(GrpcClientCallAction call, Metadata responseHeaders, ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        if (responseHeaders != null && IncludeResponseHeaders?.Invoke(CallContext.From(context)) == true)
        {
            call.ResponseHeaders = [];

            foreach (var header in responseHeaders)
            {
                call.ResponseHeaders.Add(new GrpcMetadata()
                {
                    Key = header.Key,
                    IsBinary = header.IsBinary,
                    Value = !header.IsBinary ? header.Value : null,
                    ValueBytes = header.IsBinary ? header.ValueBytes : null
                });
            }
        }
    }

    private static void HydrateResponseStatus(GrpcClientCallAction action, Status status)
    {
        action.IsSuccess = status.StatusCode == StatusCode.OK;
        action.StatusCode = status.StatusCode.ToString();
        action.StatusDetail = status.Detail;
    }

    private void HydrateResponseTrailers<TRequest, TResponse>(GrpcClientCallAction call, Metadata responseTrailers, ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        if (responseTrailers != null && IncludeTrailers?.Invoke(CallContext.From(context)) == true)
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

    protected internal virtual bool IsAuditDisabled<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        if (Configuration.AuditDisabled)
        {
            return true;
        }

        if (CallFilter == null)
        {
            return false;
        }

        return !CallFilter.Invoke(CallContext.From(context));
    }

    protected virtual IAuditScope CreateAuditScope(AuditEvent auditEvent)
    {
        var auditScopeFactory = AuditScopeFactory ?? Configuration.AuditScopeFactory;

        var auditScope = auditScopeFactory.Create(new AuditScopeOptions
        {
            AuditEvent = auditEvent,
            CreationPolicy = EventCreationPolicy,
            DataProvider = DataProvider
        });

        return auditScope;
    }

    protected virtual Task<IAuditScope> CreateAuditScopeAsync(AuditEvent auditEvent)
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


    private AuditEventGrpcClient CreateGrpcClientAuditEvent<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class
    {
        var action = new GrpcClientCallAction
        {
            MethodType = context.Method.Type.ToString(),
            MethodName = context.Method.Name,
            FullName = context.Method.FullName,
            ServiceName = context.Method.ServiceName,
            Host = context.Host,
            RequestType = typeof(TRequest).FullName,
            ResponseType = typeof(TResponse).FullName,
            Deadline = context.Options.Deadline
        };

        var methodContext = CallContext.From(context);

        if (context.Options.Headers != null && IncludeRequestHeaders?.Invoke(methodContext) == true)
        {
            action.RequestHeaders = [];

            foreach (var header in context.Options.Headers)
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

        if (IncludeRequestPayload?.Invoke(methodContext) == true)
        {
            action.Request = request;
        }

        var eventType = (EventTypeName?.Invoke(methodContext) ?? "/{service}/{method}")
            .Replace("{service}", methodContext.Method.ServiceName)
            .Replace("{method}", methodContext.Method.Name);

        var auditEvent = new AuditEventGrpcClient()
        {
            Action = action,
            EventType = eventType
        };

        return auditEvent;
    }

    #endregion

}