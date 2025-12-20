using Audit.Core;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.AzureFunctions;

/// <summary>
/// Represents the details of an Azure Function invocation, including identifiers, binding data, execution, trace, trigger information, and any exception encountered.
/// </summary>
public class AzureFunctionCall : IAuditOutput
{
    /// <summary>
    /// Gets the function ID, typically assigned by the host.
    /// This identifier is unique to a function and stable across invocations.
    /// </summary>
    public string FunctionId { get; set; }

    /// <summary>
    /// Gets the invocation ID.
    /// This identifier is unique to an invocation.
    /// </summary>
    public string InvocationId { get; set; }

    /// <summary>
    /// Describes the function being executed.
    /// </summary>
    public AzureFunctionDefinition FunctionDefinition { get; set; }

    /// <summary>
    /// Binding data information for the call context.
    /// This contains all the trigger defined metadata.
    /// </summary>
    public Dictionary<string, object> BindingData { get; set; }

    /// <summary>
    /// Distributed tracing context information.
    /// </summary>
    public AzureFunctionTrace Trace { get; set; }

    /// <summary>
    /// Trigger information for the function invocation.
    /// </summary>
    public AzureFunctionTrigger Trigger { get; set; }

    /// <summary>
    /// Exception message associated with the current operation. Null if no exception occurred.
    /// </summary>
    public string Exception { get; set; }

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess => Exception == null;

    /// <inheritdoc/>
    public string ToJson()
    {
        return Audit.Core.Configuration.JsonAdapter.Serialize(this);
    }

    /// <inheritdoc/>
    [JsonExtensionData]
    public Dictionary<string, object> CustomFields { get; set; }
}