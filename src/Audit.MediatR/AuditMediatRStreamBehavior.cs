using Audit.Core.Extensions;
using Audit.MediatR.ConfigurationApi;

using MediatR;

using System;
using System.Collections.Generic;
using System.Threading;

namespace Audit.MediatR;

/// <summary>
/// <para>MediatR stream pipeline behavior that audits streaming request handling.</para>
/// <para>Creates an audit scope, optionally captures the request payload, records each streamed response item, and logs exceptions.</para>
/// <para>Configuration options can be provided in two ways:</para>
/// <list type="bullet">
///   <item>
///     <description>Using the <see cref="IAuditMediatRConfigurator"/> via the constructor that accepts a configurator action.</description>
///   </item>
///   <item>
///     <description>By passing an <see cref="AuditMediatROptions"/> instance to the corresponding constructor.</description>
///   </item>
/// </list>
/// </summary>
/// <typeparam name="TRequest">The streaming request type implementing <see cref="IStreamRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The streamed response item type.</typeparam>
public class AuditMediatRStreamBehavior<TRequest, TResponse> : AuditBehaviorBase<TRequest, TResponse>, IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    protected internal override MediatRCallType CallType => MediatRCallType.StreamRequest;

    public AuditMediatRStreamBehavior()
    {
    }

    public AuditMediatRStreamBehavior(AuditMediatROptions options)
    {
        Options = options;
    }

    public AuditMediatRStreamBehavior(Action<IAuditMediatRConfigurator> configurator)
    {
        var config = new AuditMediatRConfigurator();
        if (configurator != null)
        {
            configurator.Invoke(config);

            Options = config.Options;
        }
    }

    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (IsAuditDisabled(request))
        {
            return next();
        }

        var callContext = new MediatRCallContext(CallType, request, typeof(TRequest), typeof(TResponse));

        var auditEvent = new AuditEventMediatR()
        {
            Call = new MediatRCallAction()
            {
                CallType = CallType.ToString(),
                RequestType = request?.GetType().GetFullTypeName() ?? typeof(TRequest).GetFullTypeName(),
                Request = Options.IncludeRequest?.Invoke(callContext) == true ? request : null,
                ResponseType = typeof(TResponse).GetFullTypeName(),
                ResponseStream = [],
                CallContext = callContext
            }
        };

        var auditScopeCreateTask = CreateAuditScopeAsync(auditEvent, callContext);
        
        return new AuditAsyncEnumerable<TResponse>(
            next(),
            auditScopeCreateTask,
            onNewItemAction: item => auditEvent.Call.ResponseStream.Add(item),
            onExceptionAction: ex => auditEvent.Call.Exception = ex.GetExceptionInfo());
    }
}