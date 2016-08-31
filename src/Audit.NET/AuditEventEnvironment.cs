using Newtonsoft.Json;

namespace Audit.Core
{
    public class AuditEventEnvironment
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
        public string Exception { get; set; }

        /// <summary>
        /// The locale name
        /// </summary>
        public string Culture { get; set; }
    }
}