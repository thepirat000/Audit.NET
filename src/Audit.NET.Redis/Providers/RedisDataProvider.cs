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
            _handler.Replace(eventId, auditEvent);
        }
    }
}
