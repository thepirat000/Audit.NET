using System.Threading.Tasks;
using Audit.Core;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Store Audit logs in a Redis database as a strings, lists, hashes, or sortedsets.
    /// </summary>
    /// <remarks>
    /// Settings:
    ///     Handler: The redis data handler (string, list, hash, sortedset, pubsub)
    /// </remarks>
    public class RedisDataProvider : AuditDataProvider
    {
        private readonly RedisProviderHandler _handler;

        public RedisDataProvider(RedisProviderHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Stores an event in a redis database
        /// </summary>
        /// <param name="auditEvent">The audit event being created.</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            return _handler.Insert(auditEvent);
        }

        /// <summary>
        /// Stores/Updates an event in a redis database, related to a previous event
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var key = _handler.GetKey(auditEvent);
            _handler.Replace(key, eventId, auditEvent);
        }

        /// <summary>
        /// Gets an audit event from a redis database
        /// </summary>
        /// <param name="eventId">The event id</param>
        public override T GetEvent<T>(object eventId)
        {
            var key = _handler.GetKey(null);
            return _handler.Get<T>(key, eventId);
        }

        /// <summary>
        /// Stores an event in a redis database asynchronously
        /// </summary>
        /// <param name="auditEvent">The audit event being created.</param>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            return await _handler.InsertAsync(auditEvent);
        }

        /// <summary>
        /// Stores/Updates an event in a redis database asynchronously, related to a previous event
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var key = _handler.GetKey(auditEvent);
            await _handler.ReplaceAsync(key, eventId, auditEvent);
        }

        /// <summary>
        /// Gets an audit event from a redis database asynchronously
        /// </summary>
        /// <param name="eventId">The event id</param>
        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            var key = _handler.GetKey(null);
            return await _handler.GetAsync<T>(key, eventId);
        }
    }
}
