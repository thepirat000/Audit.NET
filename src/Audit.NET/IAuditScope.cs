using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScope : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the data provider for this AuditScope instance.
        /// </summary>
        AuditDataProvider DataProvider { get; }

        /// <summary>
        /// Gets the event related to this scope.
        /// </summary>
        AuditEvent Event { get; }

        /// <summary>
        /// Gets the creation policy for this scope.
        /// </summary>
        EventCreationPolicy EventCreationPolicy { get; }

        /// <summary>
        /// Gets a key/value collection that can be used to share data within this Audit Scope.
        /// </summary>
        IDictionary<string, object> Items { get; }

        /// <summary>
        /// Gets the current event ID, or NULL if not yet created.
        /// </summary>
        object EventId { get; }

        /// <summary>
        /// Indicates the change type
        /// </summary>
        string EventType { get; set; }

        /// <summary>
        /// The current save mode. Useful on custom actions to determine the saving trigger.
        /// </summary>
        SaveMode SaveMode { get; }

        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        void Comment(string text);

        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        void Comment(string format, params object[] args);

        /// <summary>
        /// Discards this audit scope, so the event will not be written.
        /// </summary>
        void Discard();

        /// <summary>
        /// Manually Saves (insert/replace) the Event.
        /// Use this method to save (insert/replace) the event when CreationPolicy is set to Manual.
        /// </summary>
        void Save();

        /// <summary>
        /// Manually Saves (insert/replace) the Event asynchronously.
        /// Use this method to save (insert/replace) the event when CreationPolicy is set to Manual.
        /// </summary>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        Task SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a custom field to the event
        /// </summary>
        /// <typeparam name="TC">The type of the value.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value object.</param>
        /// <param name="serialize">if set to <c>true</c> the value will be serialized immediately.</param>
        void SetCustomField<TC>(string fieldName, TC value, bool serialize = false);

        /// <summary>
        /// Replaces the target object getter whose old/new value will be stored on the AuditEvent.Target property
        /// </summary>
        /// <param name="targetGetter">A function that returns the target</param>
        void SetTargetGetter(Func<object> targetGetter);

        /// <summary>
        /// Gets the event related to this scope of a known AuditEvent derived type. Returns null if the event is not of the specified type.
        /// </summary>
        /// <typeparam name="T">The AuditEvent derived type</typeparam>
        T EventAs<T>() where T : AuditEvent;

        /// <summary>
        /// Returns the value of an item from the Items collection, or NULL if the key does not exist or is of a different type.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        T GetItem<T>(string key);
    }
}