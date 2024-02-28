using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core.ConfigurationApi;

namespace Audit.Core.Providers
{
    /// <summary>
    /// Data provider to store the audit events in a BlockingCollection that can be accessed to consume the events.
    /// This data provider does not allow replacing events, the CreationPolicy InsertOnStartReplaceOnEnd is not allowed when using this data provider.
    /// </summary>
    public class BlockingCollectionDataProvider : AuditDataProvider
    {
        private readonly BlockingCollection<AuditEvent> _events;
        
        /// <summary>
        /// Gets the number of audit events currently stored in memory.
        /// </summary>
        public int Count => _events.Count;

        /// <summary>
        /// Creates a new instance of BlockingCollectionDataProvider.
        /// </summary>
        /// <param name="config">The configuration action</param>
        public BlockingCollectionDataProvider(Action<IBlockingCollectionProviderConfigurator> config)
        {
            var configurator = new BlockingCollectionProviderConfigurator();
            if (config != null)
            {
                config.Invoke(configurator);

                switch (configurator._collectionType)
                {
                    case 0:
                        // Queue
                        _events = configurator._extra._capacity.HasValue 
                            ? new BlockingCollection<AuditEvent>(new ConcurrentQueue<AuditEvent>(), configurator._extra._capacity.Value) 
                            : new BlockingCollection<AuditEvent>(new ConcurrentQueue<AuditEvent>());
                        break;
                    case 1:
                        // Stack
                        _events = configurator._extra._capacity.HasValue
                            ? new BlockingCollection<AuditEvent>(new ConcurrentStack<AuditEvent>(), configurator._extra._capacity.Value)
                            : new BlockingCollection<AuditEvent>(new ConcurrentStack<AuditEvent>());
                        break;
                    case 2:
                        // Bag
                        _events = configurator._extra._capacity.HasValue
                            ? new BlockingCollection<AuditEvent>(new ConcurrentBag<AuditEvent>(), configurator._extra._capacity.Value)
                            : new BlockingCollection<AuditEvent>(new ConcurrentBag<AuditEvent>());
                        break;
                }
            }
        }

        /// <summary>
        /// Creates a new instance of BlockingCollectionDataProvider.
        /// </summary>
        /// <param name="collection">The internal collection to use. By default, it will use a ConcurrentQueue.</param>
        /// <param name="capacity">The capacity of the internal collection. By default, it will use an unbounded capacity.</param>
        public BlockingCollectionDataProvider(IProducerConsumerCollection<AuditEvent> collection = null, int? capacity = null)
        {
            if (collection is not null && capacity is not null)
            {
                _events = new BlockingCollection<AuditEvent>(collection, capacity.Value);
            }
            else if (capacity.HasValue)
            {
                _events = new BlockingCollection<AuditEvent>(capacity.Value);
            }
            else if (collection is not null)
            {
                _events = new BlockingCollection<AuditEvent>(collection);
            }
            else
            {
                _events = new BlockingCollection<AuditEvent>();
            }
        }

        /// <summary>
        /// Inserts an audit event into the collection.
        /// </summary>
        /// <param name="auditEvent">The audit event to insert</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            _events.Add(auditEvent);

            return null;
        }

        /// <summary>
        /// Inserts an audit event into the collection.
        /// </summary>
        /// <param name="auditEvent">The audit event to insert</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _events.Add(auditEvent, cancellationToken);

            return Task.FromResult<object>(null);
        }
        
        /// <summary>
        /// Returns a read-only collection of audit events currently stored in memory, this will not remove audit events from the queue.
        /// </summary>
        public IList<AuditEvent> GetAllEvents()
        {
            return _events.ToList().AsReadOnly();
        }

        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed while observing the cancellation token.
        /// </summary>
        public AuditEvent Take(CancellationToken cancellationToken = default)
        {
            return _events.Take(cancellationToken);
        }

        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed while observing the cancellation token.
        /// </summary>
        public Task<AuditEvent> TakeAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => _events.Take(cancellationToken), cancellationToken);
        }

        
        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed, the timeout is reached or the cancellation token is triggered.
        /// Returns NULL if no audit event is available.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or (-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AuditEvent TryTake(int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            if (_events.TryTake(out var auditEvent, millisecondsTimeout, cancellationToken))
            {
                return auditEvent;
            }

            return default;
        }

        /// <summary>
        /// Provides a consuming Enumerable for the audit events.
        /// This method enables client code to retrieve and remove items from the audit event collection by using a foreach loop.
        /// The enumerator will continue to provide items (if any exist), otherwise the loop blocks until an item becomes available or until the CancellationToken is cancelled.
        /// </summary>
        public IEnumerable<AuditEvent> GetConsumingEnumerable(CancellationToken cancellationToken = default)
        {
            return _events.GetConsumingEnumerable(cancellationToken);
        }

        /// <summary>
        /// Gets the internal BlockingCollection used to store the audit events.
        /// </summary>
        public BlockingCollection<AuditEvent> GetBlockingCollection()
        {
            return _events;
        }
    }
}