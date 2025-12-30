using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.Core;

/// <summary>
/// Represents a sub-event that occurs at a specific point in time, including associated data and custom fields for audit
/// output.
/// </summary>
/// <remarks>The TimedEvent class provides a flexible structure for recording sub-events / annotations with time information and
/// arbitrary data. Custom fields can be added to extend the event with extra information as needed.
/// </remarks>
public class TimedEvent : IAuditOutput
{
    /// <summary>
    /// The date when the timed event occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The timestamp (in ticks) when the timed event occurred. Only set if Configuration.IncludeTimestamps is true.
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// The offset in milliseconds from the start of the parent event.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Data payload associated with the current instance.
    /// </summary>
    /// <remarks>The value can be of any type and may be null. When serializing to JSON, this property is omitted if its value is null.</remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Data { get; set; }

    /// <summary>
    /// Collection of custom fields that are not explicitly defined by the model.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> CustomFields { get; set; }

    /// <inheritdoc/>
    public string ToJson()
    {
        return Configuration.JsonAdapter.Serialize(this);
    }
}