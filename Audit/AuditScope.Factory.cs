using System;

namespace Audit.Core
{
    /// <summary>
    /// A factory of scopes.
    /// </summary>
    public static class AuditScope
    {
        /// <summary>
        /// Creates an audit scope from a reference value and and event type.
        /// When using this overload, don't forget to set the ReferenceId property before disposing.
        /// The event type is set to the type of T.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        public static AuditScope<T> Create<T>(string eventType, Func<T> target)
        {
            return new AuditScope<T>(eventType, target, null, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value.
        /// (When using this overload, don't forget to set the ReferenceId property before saving or disposing)
        /// </summary>
        /// <param name="target">The reference object getter.</param>
        public static AuditScope<T> Create<T>(Func<T> target)
        {
            return new AuditScope<T>(typeof(T).Name, target, null, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and a reference Id.
        /// </summary>
        /// <param name="target">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public static AuditScope<T> Create<T>(Func<T> target, string referenceId)
        {
            return new AuditScope<T>(typeof(T).Name, target, referenceId, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public static AuditScope<T> Create<T>(string eventType, Func<T> target, string referenceId)
        {
            return new AuditScope<T>(eventType, target, referenceId, 2);
        }
    }
}
