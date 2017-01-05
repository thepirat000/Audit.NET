using System;
using System.Runtime.CompilerServices;

namespace Audit.Core
{
    /// <summary>
    /// A factory of scopes.
    /// </summary>
    public partial class AuditScope
    {
        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CreateAndSave(string eventType, object extraFields, AuditDataProvider dataProvider = null)
        {
            new AuditScope(eventType, null, extraFields, dataProvider, null, true);
        }

        /// <summary>
        /// Creates an audit scope for a target object and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AuditScope Create(string eventType, Func<object> target)
        {
            return new AuditScope(eventType, target, null, null, null);
        }

        /// <summary>
        /// Creates an audit scope for a targer object and an event type, providing the creation policy and optionally the Data Provider.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AuditScope Create(string eventType, Func<object> target, EventCreationPolicy creationPolicy, AuditDataProvider dataProvider = null)
        {
            return new AuditScope(eventType, target, null, dataProvider, creationPolicy);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
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
        /// <param name="auditEvent">The initialized audit event to use, or NULL to create a new instance of AuditEvent.</param>
        /// <param name="skipExtraFrames">Used to indicate how many frames in the stack should be skipped to determine the calling method.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AuditScope Create(string eventType, Func<object> target, object extraFields, EventCreationPolicy creationPolicy, AuditDataProvider dataProvider = null, AuditEvent auditEvent = null, int skipExtraFrames = 0)
        {
            return new AuditScope(eventType, target, extraFields, dataProvider, creationPolicy, false, auditEvent, skipExtraFrames);
        }

    }
}
