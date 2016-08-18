using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Target object data.
    /// </summary>
    [Serializable]
    [DataContract]
    public class AuditTarget
    {
        /// <summary>
        /// The type of the object tracked
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// The value of the object tracked when the auditscope is created
        /// </summary>
        [JsonProperty("Old")]
        [DataMember]
        public object SerializedOld { get; set; }

        /// <summary>
        /// The value of the object tracked after the auditscope is saved
        /// </summary>
        [JsonProperty("New")]
        [DataMember]
        public object SerializerNew { get; set; }

        public AuditTarget()
        {
        }

        public AuditTarget(string type)
        {
            Type = type;
        }
    }
}