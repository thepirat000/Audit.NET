using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Represents the output of the audit process
    /// </summary>
    [Serializable]
    [DataContract]
    public class AuditEvent
    {
        /// <summary>
        /// Internally stores the EventId (set after inserting an event)
        /// </summary>
        [JsonIgnore]
        public object EventId { get; internal set; }
        /// <summary>
        /// The enviroment information
        /// </summary>
        [JsonProperty]
        [DataMember]
        public AuditEventEnvironment Environment { get; set; }

        /// <summary>
        /// Indicates the change type (i.e. CustomerOrder Update)
        /// </summary>
        [JsonProperty(Order = -999)]
        [DataMember]
        public string EventType { get; set; }

        /// <summary>
        /// The extension data. 
        /// This will be serialized as the keys being properties of the current object.
        /// </summary>
        [JsonExtensionData]
        [DataMember]
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// The tracked target.
        /// </summary>
        [JsonProperty("Target", NullValueHandling = NullValueHandling.Ignore)]
        [DataMember]
        public AuditTarget Target { get; set; }

        /// <summary>
        /// Comments.
        /// </summary>
        [JsonProperty("Comments", NullValueHandling = NullValueHandling.Ignore)]
        [DataMember]
        public List<string> Comments { get; set; }

        /// <summary>
        /// The date then the event started
        /// </summary>
        [JsonProperty("StartDate")]
        [DataMember]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The date then the event finished
        /// </summary>
        [JsonProperty("EndDate")]
        [DataMember]
        public DateTime EndDate { get; set; }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        /// <summary>
        /// Converts the event to its JSON representation using JSON.NET.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, JsonSettings);
        }
    }
}
