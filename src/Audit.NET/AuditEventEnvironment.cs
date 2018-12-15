using System.Collections.Generic;
using Newtonsoft.Json;

namespace Audit.Core
{
    public class AuditEventEnvironment : IAuditOutput
    {
        /// <summary>
        /// Gets or sets the name of the user responsible for the change.
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the machine.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DomainName { get; set; }

        /// <summary>
        /// The name of the method that has the audited code
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CallingMethodName { get; set; }

        /// <summary>
        /// The name of the assembly from where the audit scope was invoked
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AssemblyName { get; set; }

        /// <summary>
        /// The exception information (if any)
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }

        /// <summary>
        /// The locale name
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Culture { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes this Environment entity as a JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Audit.Core.Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an Environment entity from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Environment entity representation.</param>
        public static AuditEventEnvironment FromJson(string json)
        {
            return JsonConvert.DeserializeObject<AuditEventEnvironment>(json, Audit.Core.Configuration.JsonSettings);
        }
    }
}