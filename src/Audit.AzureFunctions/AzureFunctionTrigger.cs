using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.AzureFunctions;

/// <summary>
/// Represents the trigger configuration for an Azure Function, including its type and associated attributes.
/// </summary>
public class AzureFunctionTrigger
{
    /// <summary>
    /// Trigger type. For example: "HttpTrigger", "TimerTrigger", "BlobTrigger", "QueueTrigger", etc.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Additional attributes associated with the trigger.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> Attributes { get; set; }
}