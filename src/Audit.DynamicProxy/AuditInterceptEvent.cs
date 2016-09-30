using System.Collections.Generic;
using Newtonsoft.Json;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Represents an intercepted operation call
    /// </summary>
    public class AuditInterceptEvent
    {
        /// <summary>
        /// The class name where the operation is defined.
        /// </summary>
        [JsonProperty(Order = 1)]
        public string ClassName { get; set; }
        /// <summary>
        /// The property name (if the event is a property access).
        /// </summary>
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string PropertyName { get; set; }
        /// <summary>
        /// The event name (if the event is an EventHandler access).
        /// </summary>
        [JsonProperty(Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public string EventName { get; set; }
        /// <summary>
        /// The method name
        /// </summary>
        [JsonProperty(Order = 10)]
        public string MethodName { get; set; }
        /// <summary>
        /// A boolean indicating whether this method is async
        /// </summary>
        [JsonProperty(Order = 15)]
        public bool IsAsync { get; set; }
        /// <summary>
        /// A string indicating the Task final status
        /// </summary>
        [JsonProperty(Order = 17, NullValueHandling = NullValueHandling.Ignore)]
        public string AsyncStatus { get; set; }
        /// <summary>
        /// The class instance qualified name
        /// </summary>
        [JsonProperty(Order = 20)]
        public string InstanceQualifiedName { get; set; }
        /// <summary>
        /// The complete method signature
        /// </summary>
        [JsonProperty(Order = 30)]
        public string MethodSignature { get; set; }
        /// <summary>
        /// The arguments (input and output parameters)
        /// </summary>
        [JsonProperty(Order = 40)]
        public List<AuditInterceptArgument> Arguments { get; set; }
        /// <summary>
        /// A value indicating if the call was sucessful
        /// </summary>
        [JsonProperty(Order = 50)]
        public bool Success { get; set; }
        /// <summary>
        /// The exception description (if any)
        /// </summary>
        [JsonProperty(Order = 60, NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }
        [JsonProperty(Order = 70)]
        /// <summary>
        /// The result of the operation
        /// </summary>
        public AuditInterceptArgument Result { get; set; }
    }
}