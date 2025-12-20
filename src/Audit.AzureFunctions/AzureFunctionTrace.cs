using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.AzureFunctions;

/// <summary>
/// Represents trace context information for an Azure Function invocation, including the distributed tracing parent and additional attributes.
/// </summary>
public class AzureFunctionTrace
{
    /// <summary>
    /// Identity of the incoming invocation in a tracing system.
    /// </summary>
    public string TraceParent { get; set; }

    /// <summary>
    /// Trace state value for distributed tracing operations.
    /// </summary>
    public string TraceState { get; set; }

    /// <summary>
    /// Additional attributes associated with the trace.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> Attributes { get; set; }
}