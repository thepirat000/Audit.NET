using System;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core.Extensions;
using Audit.MediatR.ConfigurationApi;

using MediatR;

namespace Audit.MediatR;

/// <summary>
/// <para>MediatR pipeline behavior that audits request handling.</para>
/// <para>It creates an audit scope, captures request/response payloads (when enabled), and records exceptions.</para>
/// <para>You can configure it in two ways:</para>
/// <list type="bullet">
///   <item>
///     <description>Via the <see cref="IAuditMediatRConfigurator"/> using the constructor that accepts a configurator action.</description>
///   </item>
///   <item>
///     <description>By supplying an <see cref="AuditMediatROptions"/> instance to the corresponding constructor.</description>
///   </item>
/// </list>
/// </summary>
/// <typeparam name="TRequest">The request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
public class AuditMediatRBehavior<TRequest, TResponse> : AuditBehaviorBase<TRequest, TResponse>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    protected internal override MediatRCallType CallType => MediatRCallType.Request;

    public AuditMediatRBehavior()
    {
    }

    public AuditMediatRBehavior(AuditMediatROptions options)
    {
        Options = options;
    }

    public AuditMediatRBehavior(Action<IAuditMediatRConfigurator> configurator)
    {
        var config = new AuditMediatRConfigurator();
        if (configurator != null)
        {
            configurator.Invoke(config);
            
            Options = config.Options;
        }
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (IsAuditDisabled(request))
        {
            return await next.Invoke(cancellationToken);
        }

        var callContext = new MediatRCallContext(CallType, request, typeof(TRequest), typeof(TResponse));

        var auditEvent = new AuditEventMediatR()
        {
            Call = new MediatRCallAction()
            {
                CallType = CallType.ToString(),
                RequestType = request?.GetType().GetFullTypeName() ?? typeof(TRequest).GetFullTypeName(),
                ResponseType = typeof(TResponse).GetFullTypeName(),
                Request = Options.IncludeRequest?.Invoke(callContext) == true ? request : null,
                CallContext = callContext
            }
        };

        await using var auditScope = await CreateAuditScopeAsync(auditEvent, callContext);

        try
        {
            var response = await next.Invoke(cancellationToken);

            if (Options.IncludeResponse?.Invoke(callContext) == true)
            {
                auditEvent.Call.Response = response;
            }

            return response;
        }
        catch (Exception ex)
        {
            auditEvent.Call.Exception = ex.GetExceptionInfo();

            throw;
        }
    }
}