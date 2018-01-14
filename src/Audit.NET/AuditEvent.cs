using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Represents the output of the audit process
    /// </summary>
    public class AuditEvent
    {
        /// <summary>
        /// The enviroment information
        /// </summary>
        [JsonProperty]
        public AuditEventEnvironment Environment { get; set; }

        /// <summary>
        /// Indicates the change type (i.e. CustomerOrder Update)
        /// </summary>
        [JsonProperty(Order = -999)]
        public string EventType { get; set; }

        /// <summary>
        /// The extension data. 
        /// This will be serialized as the keys being properties of the current object.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// The tracked target.
        /// </summary>
        [JsonProperty("Target", NullValueHandling = NullValueHandling.Ignore)]
        public AuditTarget Target { get; set; }

        /// <summary>
        /// Comments.
        /// </summary>
        [JsonProperty("Comments", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Comments { get; set; }

        /// <summary>
        /// The date then the event started
        /// </summary>
        [JsonProperty("StartDate")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The date then the event finished
        /// </summary>
        [JsonProperty("EndDate")]
        public DateTime? EndDate { get; set; }

        ///<summary>
        /// The duration of the operation in milliseconds.
        /// </summary>
        [JsonProperty]
        public int Duration { get; set; }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// Converts the event to its JSON representation using JSON.NET.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, JsonSettings);
        }
        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        public static T FromJson<T>(string json) where T : AuditEvent
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }
        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        public static AuditEvent FromJson(string json) 
        {
            return JsonConvert.DeserializeObject<AuditEvent>(json, JsonSettings);
        }
    }
}
