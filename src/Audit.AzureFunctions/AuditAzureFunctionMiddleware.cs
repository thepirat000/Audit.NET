// ReSharper disable ConstantConditionalAccessQualifier
using Audit.Core;
using Audit.Core.Extensions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Audit.AzureFunctions.ConfigurationApi;

namespace Audit.AzureFunctions;

/// <summary>
/// Provides middleware for auditing Azure Functions executions by capturing invocation details and generating audit events during function processing.
/// </summary>
/// <remarks>
/// This middleware integrates with the Azure Functions worker pipeline to record execution data, including function parameters, trigger information, and exceptions. Audit events are created and stored using the
/// configured audit data provider. To customize auditing behavior, use the provided options or supply a configuration delegate. The middleware should be registered in the Azure Functions worker startup to enable auditing for all function invocations.
/// </remarks>
public class AuditAzureFunctionMiddleware : IFunctionsWorkerMiddleware
{
    private const string BindingAttributeKey = "bindingAttribute";

    /// <summary>
    /// Key used to identify audit event data in the context Items collection.
    /// </summary>
    public const string AuditEventKey = "__AuditEvent__";

    /// <summary>
    /// Gets the configuration options used for auditing Azure Functions.
    /// </summary>
    public AuditAzureFunctionOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the AuditAzureFunctionMiddleware class with default options.
    /// </summary>
    /// <remarks>This constructor sets the Options property to a new instance of AuditAzureFunctionOptions, providing default configuration for the middleware. Use this constructor when no custom options are required.</remarks>
    public AuditAzureFunctionMiddleware()
    {
        Options = new AuditAzureFunctionOptions();
    }

    /// <summary>
    /// Initializes a new instance of the AuditAzureFunctionMiddleware class using the specified options.
    /// </summary>
    /// <param name="options">The configuration options that control the behavior of the audit middleware.</param>
    public AuditAzureFunctionMiddleware(AuditAzureFunctionOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// Initializes a new instance of the AuditAzureFunctionMiddleware class with the specified configuration action.
    /// </summary>
    /// <remarks>Use this constructor to set up auditing for Azure Functions by providing a configuration.</remarks>
    /// <param name="configure">An action delegate that configures the audit options for Azure Functions. This delegate is invoked to customize auditing behavior.</param>
    public AuditAzureFunctionMiddleware(Action<IAuditAzureFunctionConfigurator> configure)
    {
        Options = new AuditAzureFunctionOptions(configure);
    }

    /// <inheritdoc/>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (IsAuditDisabled(context))
        {
            await next.Invoke(context);
            return;
        }
        
        var auditEvent = CreateAuditEvent(context);

        var factory = context.InstanceServices?.GetService<IAuditScopeFactory>() ?? Configuration.AuditScopeFactory;
        var dataProvider = Options.DataProvider?.Invoke(context) ?? context.InstanceServices?.GetService<IAuditDataProvider>();

        var eventType = (Options.EventType?.Invoke(context) ?? "{name}")
            .Replace("{name}", context.FunctionDefinition.Name)
            .Replace("{id}", context.FunctionId);

        await using var auditScope = await factory.CreateAsync(new AuditScopeOptions()
        {
            AuditEvent = auditEvent,
            EventType = eventType,
            CreationPolicy = Options.EventCreationPolicy,
            DataProvider = dataProvider
        });
        
        context.Items[AuditEventKey] = auditEvent;
        
        try
        {
            await next.Invoke(context);
        }
        catch (Exception ex)
        {
            auditEvent.Call.Exception = ex.GetExceptionInfo();

            throw;
        }
    }

    private bool IsAuditDisabled(FunctionContext context)
    {
        if (Configuration.AuditDisabled)
        {
            return true;
        }

        if (Options.AuditWhen == null)
        {
            return false;
        }
        
        return !Options.AuditWhen.Invoke(context);
    }

    private AuditEventAzureFunction CreateAuditEvent(FunctionContext context)
    {
        var callData = new AzureFunctionCall()
        {
            FunctionId = context.FunctionId,
            InvocationId = context.InvocationId,
            FunctionDefinition = Options.IncludeFunctionDefinition?.Invoke(context) != true ? null : GetAzureFunctionDefinition(context),
            BindingData = context.BindingContext?.BindingData.ToDictionary(k => k.Key, v => v.Value),
            Trace = new AzureFunctionTrace()
            {
                TraceParent = context.TraceContext?.TraceParent,
                TraceState = context.TraceContext?.TraceState,
                Attributes = context.TraceContext?.Attributes?.ToDictionary(k => k.Key, object (v) => v.Value)
            },
            Trigger = Options.IncludeTriggerInfo?.Invoke(context) != true ? null : GetTriggerData(context),
            CustomFields = Options.CustomFields?.Invoke(context)
        };

        var auditEvent = new AuditEventAzureFunction { Call = callData };

        return auditEvent;
    }

    internal static AzureFunctionDefinition GetAzureFunctionDefinition(FunctionContext context)
    {
        var definition = context.FunctionDefinition;

        return new AzureFunctionDefinition()
        {
            Parameters = definition.Parameters.ToList().ConvertAll(p => new AzureFunctionMetadata()
            {
                Name = p.Name,
                Type = p.Type.GetFullTypeName()
            }),
            Name = definition.Name,
            Id = definition.Id,
            EntryPoint = definition.EntryPoint,
            Assembly = definition.PathToAssembly,
            InputBindings = definition.InputBindings?.ToList().ConvertAll(b => new AzureFunctionMetadata()
            {
                Name = b.Key,
                Type = b.Value.Type,
            }),
            OutputBindings = definition.OutputBindings?.ToList().ConvertAll(b => new AzureFunctionMetadata()
            {
                Name = b.Key,
                Type = b.Value.Type
            })
        };
    }

    internal static BindingAttribute GetTriggerAttribute(FunctionContext context)
    {
        var bindingAttribute = context.FunctionDefinition.Parameters.SelectMany(p => p.Properties).FirstOrDefault(pr => pr.Key == BindingAttributeKey).Value as BindingAttribute;

        return bindingAttribute;
    }

    internal static AzureFunctionTrigger GetTriggerData(FunctionContext context)
    {
        var bindingAttribute = GetTriggerAttribute(context);
        
        if (bindingAttribute == null)
        {
            return null;
        }

        var type = bindingAttribute.GetType();

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.Name != nameof(Attribute.TypeId));

        var name = type.Name.EndsWith("Attribute") ? type.Name[..^"Attribute".Length] : type.Name;

        var result = new AzureFunctionTrigger()
        {
            Type = name,
            Attributes = []
        };

        foreach (var prop in props)
        {
            object value;
            try
            {
                value = prop.GetValue(bindingAttribute);
            }
            catch
            {
                value = null;
            }
            result.Attributes.Add(prop.Name, value);
        }

        return result;
    }
}