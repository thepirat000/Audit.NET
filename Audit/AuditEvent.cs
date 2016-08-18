using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    [Serializable]
    [DataContract]
    public class AuditEventEnvironment
    {
        /// <summary>
        /// Gets or sets the name of the user responsible for the change.
        /// </summary>
        [DataMember]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the name of the machine.
        /// </summary>
        [DataMember]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        [DataMember]
        public string DomainName { get; set; }

        /// <summary>
        /// The name of the method that has the audited code
        /// </summary>
        [JsonProperty("CallingMethodName")]
        [DataMember]
        public string CallingMethodName { get; set; }

        /// <summary>
        /// The exception information (if any)
        /// </summary>
        [JsonProperty("Exception")]
        [DataMember]
        public string Exception { get; set; }

        /// <summary>
        /// The locale name
        /// </summary>
        [JsonProperty("Culture")]
        [DataMember]
        public string Culture { get; set; }
    }

    /// <summary>
        /// Represents the output of the audit process
        /// </summary>
        [Serializable]
    [DataContract]
    public class AuditEvent
    {
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
        /// Indicates the reference Identifier for the change (i.e. The CustomerOrder Id)
        /// </summary>
        [JsonProperty(Order = -999)]
        [DataMember]
        public string ReferenceId { get; set; }

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
        [JsonProperty("Target")]
        [DataMember]
        public AuditTarget Target { get; set; }

        /// <summary>
        /// Comments.
        /// </summary>
        [JsonProperty("Comments")]
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
