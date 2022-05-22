using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

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
        /// <param name="configurationOptions">The redis configuration options.
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderString(ConfigurationOptions configurationOptions, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer, Func<AuditEvent, int> dbIndexBuilder, List<Func<IBatch, AuditEvent, Task>> extraTasks)
            : base(configurationOptions, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        { }

        /// <summary>
        /// Creates new redis provider that uses a Redis Sorted Set to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderString(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer, Func<AuditEvent, int> dbIndexBuilder, List<Func<IBatch, AuditEvent, Task>> extraTasks)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        { }

        public override object Insert(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            StringSet(key, auditEvent);
            return key;
        }

        public override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            await StringSetAsync(key, auditEvent);
            return key;
        }

        public override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            StringSet(key, auditEvent);
        }

        public override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            await StringSetAsync(key, auditEvent);
        }

        public override T Get<T>(string key, object subKey)
        {
            return StringGet<T>(key);
        }

        public override async Task<T> GetAsync<T>(string key, object subKey)
        {
            return await StringGetAsync<T>(key);
        }

        private void StringSet(string key, AuditEvent auditEvent)
        {
            var tasks = ExecStringSetBatch(key, auditEvent);
            Task.WaitAll(tasks);
        }

        private async Task StringSetAsync(string key, AuditEvent auditEvent)
        {
            var tasks = ExecStringSetBatch(key, auditEvent);
            await Task.WhenAll(tasks);
        }

        private Task[] ExecStringSetBatch(string key, AuditEvent auditEvent)
        {
            var db = GetDatabase(auditEvent);
            var value = GetValue(auditEvent);
            var batch = db.CreateBatch();
            var tasks = new List<Task>();
            tasks.Add(batch.StringSetAsync(key, value, TimeToLive));
            OnBatchExecuting(batch, tasks, auditEvent);
            batch.Execute();
            return tasks.ToArray();
        }

        private T StringGet<T>(string key) where T : AuditEvent
        {
            var db = GetDatabase(null);
            var value = db.StringGet(key);
            return FromValue<T>(value);
        }

        private async Task<T> StringGetAsync<T>(string key) where T : AuditEvent
        {
            var db = GetDatabase(null);
            var value = await db.StringGetAsync(key);
            return FromValue<T>(value);
        }
    }
}