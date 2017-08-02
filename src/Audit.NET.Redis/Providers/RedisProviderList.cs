using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Stores the audit events in a Redis List
    /// </summary>
    public class RedisProviderList : RedisProviderHandler
    {
        private readonly long _maxLength;

        /// <summary>
        /// Creates new redis provider that uses a Redis List to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="maxLength">Maximum quantity of events that the list will store. Older elements will be deleted. Default is 0 for no-limit.</param>
        public RedisProviderList(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            long maxLength)
            : base(connectionString, keyBuilder, timeToLive, serializer)
        {
            _maxLength = maxLength;
        }

        internal override object Insert(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Push(eventId, auditEvent);
            return eventId;
        }

        internal override void Replace(object eventId, AuditEvent auditEvent)
        {
            // List values cannot be properly updated. This will insert a new member to the list.
            Push((Guid)eventId, auditEvent);
        }

        private void Push(Guid eventId, AuditEvent auditEvent)
        {
            auditEvent.CustomFields[RedisEventIdField] = eventId;
            var tasks = new List<Task>();
            var key = GetKey(auditEvent);
            var value = GetValue(auditEvent);
            var batch = GetDatabase().CreateBatch();
            tasks.Add(batch.ListLeftPushAsync(key, value));
            if (_maxLength > 0)
            {
                tasks.Add(batch.ListTrimAsync(key, 0, _maxLength - 1));
            }
            if (TimeToLive.HasValue)
            {
                tasks.Add(batch.KeyExpireAsync(key, TimeToLive));
            }
            batch.Execute();
            Task.WaitAll(tasks.ToArray());
        }
    }
}