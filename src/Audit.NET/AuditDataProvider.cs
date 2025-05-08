using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    /// <summary>
    /// Base class for the Audit Data Providers that are responsible for saving and retrieving audit events.
    /// </summary>
    public abstract class AuditDataProvider : IAuditDataProvider
    {
        /// <summary>
        /// Override this method to provide a different cloning method for the values that need to be pre-serialized before saving.
        /// (old target value and custom fields)
        /// </summary>
        /// <param name="value">The value to clone</param>
        /// <param name="auditEvent">The audit event associated to the value being serialized</param>
        public virtual object CloneValue<T>(T value, AuditEvent auditEvent)
        {
            if (value is null)
            {
                return null;
            }
            
            if (value is string)
            {
                return value;
            }

            if (value.GetType().IsPrimitive)
            {
                return value;
            }

            if (value is ICloneable cloneable)
            {
                return cloneable.Clone();
            }
            
            return Configuration.JsonAdapter.Deserialize(Configuration.JsonAdapter.Serialize(value), value.GetType());
        }

        /// <summary>
        /// Insert an event to the data source returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        public abstract object InsertEvent(AuditEvent auditEvent);

        /// <summary>
        /// Insert an event to the data source returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public virtual async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            // Default implementation calls the sync operation
            return await Task.Factory.StartNew(() => InsertEvent(auditEvent), cancellationToken);
        }
        
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
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// Override this method to replace the specified audit event on the data source.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public virtual async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            // Default implementation calls the sync operation
            await Task.Factory.StartNew(() => ReplaceEvent(eventId, auditEvent), cancellationToken);
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
        /// Retrieves a saved audit event from its id.
        /// Override this method to provide a way to access the audit events by id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        public virtual T GetEvent<T>(object eventId) where T : AuditEvent
        {
            throw new NotImplementedException($"GetEvent is not implemented on {GetType().Name}");
        }

        /// <summary>
        /// Asynchronously retrieves a saved audit event from its id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public async Task<AuditEvent> GetEventAsync(object eventId, CancellationToken cancellationToken = default)
        {
            return await GetEventAsync<AuditEvent>(eventId, cancellationToken);
        }

        /// <summary>
        /// Asynchronously retrieves a saved audit event from its id.
        /// Override this method to provide a way to access the audit events by id.
        /// </summary>
        /// <param name="eventId">The event id being retrieved.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public virtual async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            // Default implementation calls the sync operation
            return await Task.Factory.StartNew(() => GetEvent<T>(eventId), cancellationToken);
        }
    }
}

