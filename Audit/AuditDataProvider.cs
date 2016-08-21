using System;
using Newtonsoft.Json.Linq;

namespace Audit.Core
{
    /// <summary>
    /// Base class for the persistence classes.
    /// </summary>
    public abstract class AuditDataProvider 
    {
        private object _eventId;

        /// <summary>
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// Override this method to save the specified audit event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public virtual void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
        }

        /// <summary>
        /// Override this method to provide a different serialization method for the values that need to be serialized before saving.
        /// (old target value & custom fields)
        /// </summary>
        public virtual object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            return JToken.FromObject(value);
        }

        /// <summary>
        /// Insert an event returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        public abstract object InsertEvent(AuditEvent auditEvent);

        /// <summary>
        /// Gets the creation policy.
        /// </summary>
        public virtual EventCreationPolicy CreationPolicy { get; set; }

        /// <summary>
        /// Executed when the scope is initialized.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public virtual void Init(AuditEvent auditEvent)
        {
            if (CreationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || CreationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                _eventId = InsertEvent(auditEvent);
            }
        }

        /// <summary>
        /// Executed when the scope should be saved.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public virtual void End(AuditEvent auditEvent)
        {
            if (CreationPolicy == EventCreationPolicy.InsertOnEnd)
            {
                _eventId = InsertEvent(auditEvent);
            }
            else if (CreationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd)
            {
                ReplaceEvent(_eventId, auditEvent);
            }
            else if (CreationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                _eventId = InsertEvent(auditEvent);
            }
        }
    }
}
