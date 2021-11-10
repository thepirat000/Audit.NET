using System.Collections.Generic;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Represents an intercepted operation call
    /// </summary>
    public class InterceptEvent
    {
        /// <summary>
        /// The class name where the operation is defined.
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// The property name (if the event is a property access).
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// The event name (if the event is an EventHandler access).
        /// </summary>
        public string EventName { get; set; }
        /// <summary>
        /// The method name
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// A boolean indicating whether this method is async
        /// </summary>
        public bool IsAsync { get; set; }
        /// <summary>
        /// A string indicating the Task final status
        /// </summary>
        public string AsyncStatus { get; set; }
        /// <summary>
        /// The class instance qualified name
        /// </summary>
        public string InstanceQualifiedName { get; set; }
        /// <summary>
        /// The complete method signature
        /// </summary>
        public string MethodSignature { get; set; }
        /// <summary>
        /// The arguments (input and output parameters)
        /// </summary>
        public List<AuditInterceptArgument> Arguments { get; set; }
        /// <summary>
        /// A value indicating if the call was sucessful
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// The exception description (if any)
        /// </summary>
        public string Exception { get; set; }
        /// <summary>
        /// The result of the operation
        /// </summary>
        public AuditInterceptArgument Result { get; set; }
    }
}