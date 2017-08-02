using System;
using System.Collections.Generic;
using System.Diagnostics;
using Audit.Core;
using System.Threading.Tasks;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Stores the audit events as Redis Hashes
    /// </summary>
    public class RedisProviderHash : RedisProviderHandler
    {
        private readonly Func<AuditEvent, string> _fieldBuilder;

        /// <summary>
        /// Creates new redis provider that uses Redis Hashes to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TLL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="fieldBuilder">A function that returns the hash field to use.</param>
        public RedisProviderHash(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            Func<AuditEvent, string> fieldBuilder)
            : base(connectionString, keyBuilder, timeToLive, serializer)
        {
            _fieldBuilder = fieldBuilder;
        }

        internal override object Insert(AuditEvent auditEvent)
        {
            if (_fieldBuilder == null)
            {
                throw new ArgumentException("The hash field was not provided");
            }
            var field = _fieldBuilder.Invoke(auditEvent);
            HashSet(field, auditEvent);
            return field;
        }

        internal override void Replace(object eventId, AuditEvent auditEvent)
        {
            var field = (string)eventId;
            HashSet(field, auditEvent);
        }

        private void HashSet(string field, AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            var value = GetValue(auditEvent);
            var batch = GetDatabase().CreateBatch();
            var tasks = new List<Task>();
            tasks.Add(batch.HashSetAsync(key, field, value));
            if (TimeToLive.HasValue)
            {
                tasks.Add(batch.KeyExpireAsync(key, TimeToLive));
            }
            batch.Execute();
            Task.WaitAll(tasks.ToArray());
        }
    }
}