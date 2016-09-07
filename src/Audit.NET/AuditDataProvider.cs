using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Base class for the persistence classes.
    /// </summary>
    public abstract class AuditDataProvider
    {
        private static JsonSerializer _defaultSerializer = JsonSerializer.Create(new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

        /// <summary>
        /// Override this method to provide a different serialization method for the values that need to be serialized before saving.
        /// (old target value and custom fields)
        /// </summary>
        public virtual object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            return JToken.FromObject(value, _defaultSerializer);
        }

        /// <summary>
        /// Insert an event to the data source returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        public abstract object InsertEvent(AuditEvent auditEvent);

        /// <summary>
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// Override this method to replace the specified audit event on the data source.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public virtual void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
        }
    }
}

