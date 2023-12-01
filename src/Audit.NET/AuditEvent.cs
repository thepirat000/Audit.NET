using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace Audit.Core
{
    /// <summary>
    /// Represents the output of the audit process
    /// </summary>
    public class AuditEvent : IAuditOutput
    {
        /// <summary>
        /// Indicates the change type (i.e. CustomerOrder Update)
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// The enviroment information
        /// </summary>
#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public AuditEventEnvironment Environment { get; set; }
        
#if NET5_0_OR_GREATER
        /// <summary>
        /// The current distributed tracing activity information 
        /// </summary>
        public AuditActivityTrace Activity { get; set; }
#endif

        /// <summary>
        /// The extension data. 
        /// This will be serialized as the keys being properties of the current object.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// The tracked target.
        /// </summary>
#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public AuditTarget Target { get; set; }

        /// <summary>
        /// Comments.
        /// </summary>
#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public List<string> Comments { get; set; }

        /// <summary>
        /// The date then the event started
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The date then the event finished
        /// </summary>
        public DateTime? EndDate { get; set; }

        ///<summary>
        /// The duration of the operation in milliseconds.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Converts the event to its JSON representation using JSON.NET.
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }

        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        /// <param name="json">JSON string with the audit event representation.</param>
        public static T FromJson<T>(string json) where T : AuditEvent
        {
            return Configuration.JsonAdapter.Deserialize<T>(json);
        }

        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        /// <param name="json">JSON string with the audit event representation.</param>
        public static AuditEvent FromJson(string json) 
        {
            return Configuration.JsonAdapter.Deserialize<AuditEvent>(json);
        }
    }
}
