using System;
using System.Reflection;
using Audit.Core;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Settings for an interception.
    /// </summary>
    public class InterceptionSettings
    {
        /// <summary>
        /// Gets or sets the type of the event. Default is "{class}.{method}".
        /// Can include the following placeholders:
        ///  - {class}: Replaced by the class name
        ///  - {method}: Replaced by the method name
        /// </summary>
        /// <value>The type of the event.</value>
        public string EventType { get; set; } = "{class}.{method}";
        /// <summary>
        /// Gets or sets a value indicating whether the audit should ignore the property getters and setters.
        /// If <c>true</c>, the property accesses will not be logged.
        /// </summary>
        public bool IgnoreProperties { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the audit should ignore the event attach and dettach actions.
        /// If <c>true</c>, the event accesses will not be logged.
        /// </summary>
        public bool IgnoreEvents { get; set; }
        /// <summary>
        /// Gets or sets the audit data provider to use.
        /// </summary>
        public IAuditDataProvider AuditDataProvider { get; set; }
        /// <summary>
        /// Gets or sets the methods filter, a function that returns true for the methods to be included or false otherwise.
        /// By default all methods are included.
        /// </summary>
        public Func<MethodInfo, bool> MethodFilter { get; set; }
        /// <summary>
        /// Gets or sets the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
        /// </summary>
        public EventCreationPolicy? EventCreationPolicy { get; set; }
        /// <summary>
        /// Gets or sets the custom audit scope factory. Default is NULL to use the general AuditScopeFactory.
        /// </summary>
        public IAuditScopeFactory AuditScopeFactory  { get; set; }
    }
}