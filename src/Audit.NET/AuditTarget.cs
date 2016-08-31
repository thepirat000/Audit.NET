using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Target object data.
    /// </summary>
    public class AuditTarget
    {
        /// <summary>
        /// The type of the object tracked
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The value of the object tracked when the auditscope is created
        /// </summary>
        [JsonProperty("Old")]
        public object SerializedOld { get; set; }

        /// <summary>
        /// The value of the object tracked after the auditscope is saved
        /// </summary>
        [JsonProperty("New")]
        public object SerializedNew { get; set; }
    }
}