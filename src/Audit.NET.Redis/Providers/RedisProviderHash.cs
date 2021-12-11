using System;
using System.Collections.Generic;
using System.Diagnostics;
using Audit.Core;
using System.Threading.Tasks;
using StackExchange.Redis;

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
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="fieldBuilder">A function that returns the hash field to use.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        public RedisProviderHash(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer,
            Func<AuditEvent, string> fieldBuilder,
            Func<AuditEvent, int> dbIndexBuilder)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder)
        {
            _fieldBuilder = fieldBuilder;
        }

        internal override object Insert(AuditEvent auditEvent)
        {
            if (_fieldBuilder == null)
            {
                throw new ArgumentException("The hash field was not provided");
            }
            var key = GetKey(auditEvent);
            var field = _fieldBuilder.Invoke(auditEvent);
            HashSet(key, field, auditEvent);
            return field;
        }

        internal override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            HashSet(key, subKey, auditEvent);
        }

        internal override T Get<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            var value = db.HashGet(key, (string)subKey);
            if (value.HasValue)
            {
                return FromValue<T>(value);
            }
            return null;
        }

        internal override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            if (_fieldBuilder == null)
            {
                throw new ArgumentException("The hash field was not provided");
            }
            var key = GetKey(auditEvent);
            var field = _fieldBuilder.Invoke(auditEvent);
            await HashSetAsync(key, field, auditEvent);
            return field;
        }

        internal override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            await HashSetAsync(key, subKey, auditEvent);
        }

        internal override async Task<T> GetAsync<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            var value = await db.HashGetAsync(key, (string)subKey);
            if (value.HasValue)
            {
                return FromValue<T>(value);
            }
            return null;
        }

        private void HashSet(string key, object subKey, AuditEvent auditEvent)
        {
            var tasks = ExecInsertBatch(key, subKey, auditEvent);
            Task.WaitAll(tasks);
        }

        private async Task HashSetAsync(string key, object subKey, AuditEvent auditEvent)
        {
            var tasks = ExecInsertBatch(key, subKey, auditEvent);
            await Task.WhenAll(tasks);
        }

        private Task[] ExecInsertBatch(string key, object subKey, AuditEvent auditEvent)
        {
            var value = GetValue(auditEvent);
            var batch = GetDatabase(auditEvent).CreateBatch();
            var tasks = new List<Task>();
            tasks.Add(batch.HashSetAsync(key, (string)subKey, value));
            if (TimeToLive.HasValue)
            {
                tasks.Add(batch.KeyExpireAsync(key, TimeToLive));
            }
            batch.Execute();
            return tasks.ToArray();
        }
    }
}