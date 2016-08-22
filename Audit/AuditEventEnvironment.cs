using System;
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
        [DataMember]
        public string CallingMethodName { get; set; }

        /// <summary>
        /// The name of the assembly from where the audit scope was invoked
        /// </summary>
        [DataMember]
        public string AssemblyName { get; set; }

        /// <summary>
        /// The exception information (if any)
        /// </summary>
        [DataMember]
        public string Exception { get; set; }

        /// <summary>
        /// The locale name
        /// </summary>
        [DataMember]
        public string Culture { get; set; }
    }
}