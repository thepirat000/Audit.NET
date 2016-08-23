using System;

namespace Audit.Core
{
    /// <summary>
    /// A factory of scopes.
    /// </summary>
    public partial class AuditScope
    {
        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        public static AuditScope Create(string eventType, Func<object> target)
        {
            return new AuditScope(eventType, target, null, null, null);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        public static AuditScope Create(string eventType, Func<object> target, EventCreationPolicy creationPolicy, AuditDataProvider dataProvider = null)
        {
            return new AuditScope(eventType, target, null, dataProvider, creationPolicy);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields will be merged into the audit event.</param>
        public static AuditScope Create(string eventType, Func<object> target, object extraFields)
        {
            return new AuditScope(eventType, target, extraFields, null, null);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields will be merged into the audit event.</param>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        public static AuditScope Create(string eventType, Func<object> target, object extraFields, EventCreationPolicy creationPolicy, AuditDataProvider dataProvider = null)
        {
            return new AuditScope(eventType, target, extraFields, dataProvider, creationPolicy);
        }

    }
}
