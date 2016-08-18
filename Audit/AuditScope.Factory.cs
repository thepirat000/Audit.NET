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
        /// <param name="reference">The reference object getter.</param>
        public static AuditScope<T> Create<T>(string eventType, Func<T> reference)
        {
            return new AuditScope<T>(eventType, reference, (Func<string>)null, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value.
        /// (When using this overload, don't forget to set the ReferenceId property before saving or disposing)
        /// </summary>
        /// <param name="reference">The reference object getter.</param>
        public static AuditScope<T> Create<T>(Func<T> reference)
        {
            return new AuditScope<T>(typeof(T).Name, reference, (Func<string>)null, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a way to get a reference Id.
        /// Use this overload when the reference id is not available until the scope is saved/disposed.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceIdGetter">The reference id getter.</param>
        public static AuditScope<T> Create<T>(string eventType, Func<T> reference, Func<string> referenceIdGetter)
        {
            return new AuditScope<T>(eventType, reference, referenceIdGetter, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and a way to get a reference Id.
        /// Use this overload when the reference id is not available until the scope is saved/disposed.
        /// </summary>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceIdGetter">The reference id getter.</param>
        public static AuditScope<T> Create<T>(Func<T> reference, Func<string> referenceIdGetter)
        {
            return new AuditScope<T>(typeof(T).Name, reference, referenceIdGetter, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and a reference Id.
        /// </summary>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public static AuditScope<T> Create<T>(Func<T> reference, string referenceId)
        {
            return new AuditScope<T>(typeof(T).Name, reference, referenceId, 2);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public static AuditScope<T> Create<T>(string eventType, Func<T> reference, string referenceId)
        {
            return new AuditScope<T>(eventType, reference, referenceId, 2);
        }
    }
}
