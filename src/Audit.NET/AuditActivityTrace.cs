#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.Core
{
    public class AuditActivityTrace : IAuditOutput
    {
        /// <summary>
        /// Date and time when the Activity started
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// SPAN part of the Id
        /// </summary>
        public string SpanId { get; set; }

        /// <summary>
        /// TraceId part of the Id
        /// </summary>
        public string TraceId { get; set; }

        /// <summary>
        /// Id of the activity's parent
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Operation name
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// List of tags (key/value pairs) associated to the activity
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AuditActivityTag> Tags { get; set; }

        /// <summary>
        /// List of events (timestamped messages) attached to the activity
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AuditActivityEvent> Events { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// Serializes this Activity Info entity as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }

        /// <summary>
        /// Parses an AuditActivityInfo entity from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the AuditActivityInfo entity representation.</param>
        public static AuditActivityTrace FromJson(string json)
        {
            return Configuration.JsonAdapter.Deserialize<AuditActivityTrace>(json);
        }
    }

    public class AuditActivityTag
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }

    public class AuditActivityEvent
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Name { get; set; }
    }
}
#endif