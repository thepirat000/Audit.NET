using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DomainName { get; set; }

        /// <summary>
        /// The name of the method that has the audited code
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CallingMethodName { get; set; }

        /// <summary>
        /// The full stack trace
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string StackTrace { get; set; }

        /// <summary>
        /// The name of the assembly from where the audit scope was invoked
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AssemblyName { get; set; }

        /// <summary>
        /// The exception information (if any)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Exception { get; set; }

        /// <summary>
        /// The locale name
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Culture { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes this Environment entity as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Environment entity from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Environment entity representation.</param>
        public static AuditEventEnvironment FromJson(string json)
        {
            return Configuration.JsonAdapter.Deserialize<AuditEventEnvironment>(json);
        }
    }
}