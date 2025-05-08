using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core;

public interface IAuditDataProvider
{
    /// <summary>
    /// Override this method to provide a different cloning method for the values that need to be pre-serialized before saving.
    /// (old target value and custom fields)
    /// </summary>
    /// <param name="value">The value to clone</param>
    /// <param name="auditEvent">The audit event associated to the value being serialized</param>
    object CloneValue<T>(T value, AuditEvent auditEvent);

    /// <summary>
    /// Insert an event to the data source returning the event id generated
    /// </summary>
    /// <param name="auditEvent">The audit event being inserted.</param>
    object InsertEvent(AuditEvent auditEvent);

    /// <summary>
    /// Insert an event to the data source returning the event id generated
    /// </summary>
    /// <param name="auditEvent">The audit event being inserted.</param>
    /// <param name="cancellationToken">The Cancellation Token.</param>
    Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified audit event.
    /// Triggered when the scope is saved.
    /// Override this method to replace the specified audit event on the data source.
    /// </summary>
    /// <param name="auditEvent">The audit event.</param>
    /// <param name="eventId">The event id being replaced.</param>
    void ReplaceEvent(object eventId, AuditEvent auditEvent);

    /// <summary>
    /// Saves the specified audit event.
    /// Triggered when the scope is saved.
    /// Override this method to replace the specified audit event on the data source.
    /// </summary>
    /// <param name="auditEvent">The audit event.</param>
    /// <param name="eventId">The event id being replaced.</param>
    /// <param name="cancellationToken">The Cancellation Token.</param>
    Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a saved audit event from its id.
    /// </summary>
    /// <param name="eventId">The event id being retrieved.</param>
    AuditEvent GetEvent(object eventId);

    /// <summary>
    /// Retrieves a saved audit event from its id.
    /// Override this method to provide a way to access the audit events by id.
    /// </summary>
    /// <param name="eventId">The event id being retrieved.</param>
    T GetEvent<T>(object eventId) where T : AuditEvent;

    /// <summary>
    /// Asynchronously retrieves a saved audit event from its id.
    /// </summary>
    /// <param name="eventId">The event id being retrieved.</param>
    /// <param name="cancellationToken">The Cancellation Token.</param>
    Task<AuditEvent> GetEventAsync(object eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a saved audit event from its id.
    /// Override this method to provide a way to access the audit events by id.
    /// </summary>
    /// <param name="eventId">The event id being retrieved.</param>
    /// <param name="cancellationToken">The Cancellation Token.</param>
    Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default) where T : AuditEvent;
}