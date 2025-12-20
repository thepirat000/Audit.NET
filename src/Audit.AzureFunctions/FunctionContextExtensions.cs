using Audit.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Audit.AzureFunctions;

public static class FunctionContextExtensions
{
    /// <summary>
    /// Retrieves the audit event associated with the specified Azure Function execution context, if available.
    /// </summary>
    /// <remarks>This method returns <see langword="null"/> if the audit event has not been set in the context. Typically, the audit event is added by the audit middleware during function invocation.</remarks>
    /// <param name="context">The function execution context from which to retrieve the audit event. Cannot be null.</param>
    /// <returns>An instance of <see cref="AuditEventAzureFunction"/> if an audit event is present in the context; otherwise, <see langword="null"/>.</returns>
    public static AuditEventAzureFunction GetAuditEvent(this FunctionContext context)
    {
        if (context.Items.TryGetValue(AuditAzureFunctionMiddleware.AuditEventKey, out var auditEventObj)
            && auditEventObj is AuditEventAzureFunction auditEvent)
        {
            return auditEvent;
        }

        return null;
    }

    /// <summary>
    /// Retrieves the audit-related Azure function call information associated with the specified function execution context.
    /// </summary>
    /// <remarks>Use this method to access audit metadata for the current function invocation, such as the function name, input parameters, and execution details. Returns <c>null</c> if no audit event is present in the context.</remarks>
    /// <param name="context">The function execution context from which to obtain the audit function call information. Cannot be null.</param>
    /// <returns>An instance of <see cref="AzureFunctionCall"/> containing details of the audit function call if available; otherwise, <c>null</c>.</returns>
    public static AzureFunctionCall GetAuditFunctionCall(this FunctionContext context)
    {
        return GetAuditEvent(context)?.Call;
    }

    /// <summary>
    /// Retrieves the audit scope associated with the specified function execution context.
    /// </summary>
    /// <remarks>Use this method to access audit information for the current function execution, such as tracking changes or logging activity. The returned audit scope may be <c>null</c> if auditing is not configured for the context.</remarks>
    /// <param name="context">The function execution context from which to obtain the audit scope. Cannot be null.</param>
    /// <returns>An <see cref="IAuditScope"/> representing the audit scope for the given context, or <c>null</c> if no audit scope is available.</returns>
    public static IAuditScope GetAuditScope(this FunctionContext context)
    {
        var auditEvent = GetAuditEvent(context);

        return auditEvent?.GetScope();
    }

    /// <summary>
    /// Retrieves the definition details of the current Azure Function from the specified execution context.
    /// </summary>
    /// <param name="context">The function execution context from which to obtain the Azure Function definition. Cannot be null.</param>
    /// <returns>An <see cref="AzureFunctionDefinition"/> instance containing metadata about the current Azure Function, such as its name and entry point.</returns>
    public static AzureFunctionDefinition GetAzureFunctionDefinition(this FunctionContext context)
    {
        return AuditAzureFunctionMiddleware.GetAzureFunctionDefinition(context);
    }

    /// <summary>
    /// Retrieves the trigger binding attribute associated with the specified Azure Functions execution context.
    /// </summary>
    /// <param name="context">The function execution context from which to obtain the trigger binding attribute. Cannot be null.</param>
    /// <returns>The trigger binding attribute for the function invocation, or null if no trigger attribute is found.</returns>
    public static BindingAttribute GetTriggerAttribute(this FunctionContext context)
    {
        return AuditAzureFunctionMiddleware.GetTriggerAttribute(context);
    }

    /// <summary>
    /// Retrieves trigger data associated with the specified Azure Function execution context.
    /// </summary>
    /// <param name="context">The function execution context from which to obtain trigger data. Cannot be null.</param>
    /// <returns>An instance of <see cref="AzureFunctionTrigger"/> containing information about the function's trigger. Returns null if trigger data is unavailable.</returns>
    public static AzureFunctionTrigger GetTriggerData(this FunctionContext context)
    {
        return AuditAzureFunctionMiddleware.GetTriggerData(context);
    }
}