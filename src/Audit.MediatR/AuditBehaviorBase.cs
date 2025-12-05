using System.Threading.Tasks;
using Audit.Core;
using Audit.MediatR.ConfigurationApi;

namespace Audit.MediatR;

/// <summary>
/// Base class for MediatR audit behaviors
/// </summary>
public abstract class AuditBehaviorBase<TRequest, TResponse>
{
    public AuditMediatROptions Options { get; set; }

    protected internal abstract MediatRCallType CallType { get; }

    protected internal Task<IAuditScope> CreateAuditScopeAsync(AuditEvent auditEvent, MediatRCallContext callContext)
    {
        var auditScopeFactory = Options.AuditScopeFactory ?? Core.Configuration.AuditScopeFactory;

        var auditScope = auditScopeFactory.CreateAsync(new AuditScopeOptions
        {
            AuditEvent = auditEvent,
            CreationPolicy = Options.EventCreationPolicy,
            DataProvider = Options.DataProvider?.Invoke(callContext)
        });

        return auditScope;
    }

    protected internal bool IsAuditDisabled(TRequest request)
    {
        if (Core.Configuration.AuditDisabled)
        {
            return true;
        }

        if (Options.CallFilter == null)
        {
            return false;
        }

        return !Options.CallFilter.Invoke(new MediatRCallContext(CallType, request, typeof(TRequest), typeof(TResponse)));
    }

}