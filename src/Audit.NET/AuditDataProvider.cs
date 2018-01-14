using System;
using System.Threading.Tasks;
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
            throw new NotImplementedException($"ReplaceEvent is not implemented on {GetType().Name}");
        }

        /// <summary>
        /// Retrieves a saved audit event from its id.
        /// Override this method to provide a way to access the audit events by id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        public virtual T GetEvent<T>(object eventId) where T : AuditEvent
        {
            throw new NotImplementedException($"GetEvent is not implemented on {GetType().Name}");
        }

        /// <summary>
        /// Insert an event to the data source returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        public virtual async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            // Default implementation calls the sync operation
            return await Task.Factory.StartNew(() => InsertEvent(auditEvent));
        }

        /// <summary>
        /// Asychronously retrieves a saved audit event from its id.
        /// Override this method to provide a way to access the audit events by id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        public virtual async Task<T> GetEventAsync<T>(object eventId) where T : AuditEvent
        {
            // Default implementation calls the sync operation
            return await Task.Factory.StartNew(() => GetEvent<T>(eventId));
        }

        /// <summary>
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// Override this method to replace the specified audit event on the data source.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public virtual async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            // Default implementation calls the sync operation
            await Task.Factory.StartNew(() => ReplaceEvent(eventId, auditEvent));
        }

        /// <summary>
        /// Retrieves a saved audit event from its id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        public AuditEvent GetEvent(object eventId)
        {
            return GetEvent<AuditEvent>(eventId);
        }

        /// <summary>
        /// Asynchronously retrieves a saved audit event from its id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        public async Task<AuditEvent> GetEventAsync(object eventId)
        {
            return await GetEventAsync<AuditEvent>(eventId);
        }
    }
}

