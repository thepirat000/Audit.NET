using System;
using Audit.Core;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Stores the audit events as Redis Strings
    /// </summary>
    public class RedisProviderString : RedisProviderHandler
    {
        /// <summary>
        /// Creates new redis provider that uses a Redis Sorted Set to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        public RedisProviderString(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer)
            : base(connectionString, keyBuilder, timeToLive, serializer)
        { }

        internal override object Insert(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            StringSet(key, auditEvent);
            return key;
        }

        internal override void Replace(object eventId, AuditEvent auditEvent)
        {
            var key = (string)eventId;
            StringSet(key, auditEvent);
        }

        private void StringSet(string key, AuditEvent auditEvent)
        {
            var db = GetDatabase();
            var value = GetValue(auditEvent);
            db.StringSet(key, value, TimeToLive);
        }
    }
}