using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Stores the audit events in a Redis List
    /// </summary>
    public class RedisProviderList : RedisProviderHandler
    {
        protected long MaxLength { get; set; }

        /// <summary>
        /// Creates new redis provider that uses a Redis List to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="maxLength">Maximum quantity of events that the list will store. Older elements will be deleted. Default is 0 for no-limit.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderList(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer,
            long maxLength,
            Func<AuditEvent, int> dbIndexBuilder,
            List<Func<IBatch, AuditEvent, Task>> extraTasks)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        {
            MaxLength = maxLength;
        }

        public override object Insert(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Push(eventId, auditEvent);
            return eventId;
        }

        public override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            // List values cannot be properly updated. This will insert a new member to the list.
            Push((Guid)subKey, auditEvent);
        }

        public override T Get<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            foreach(var item in db.ListRange(key))
            {
                if (item.HasValue)
                {
                    var auditEvent = FromValue<T>(item);
                    if (auditEvent != null && subKey.ToString().Equals(auditEvent.CustomFields[RedisEventIdField]?.ToString()))
                    {
                        return auditEvent;
                    }
                }
            }
            return null;
        }

        public override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            await PushAsync(eventId, auditEvent);
            return eventId;
        }

        public override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            // List values cannot be properly updated. This will insert a new member to the list.
            await PushAsync((Guid)subKey, auditEvent);
        }

        public override async Task<T> GetAsync<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            foreach (var item in await db.ListRangeAsync(key))
            {
                if (item.HasValue)
                {
                    var auditEvent = FromValue<T>(item);
                    if (auditEvent != null && subKey.ToString().Equals(auditEvent.CustomFields[RedisEventIdField]?.ToString()))
                    {
                        return auditEvent;
                    }
                }
            }
            return null;
        }

        private void Push(Guid eventId, AuditEvent auditEvent)
        {
            var tasks = ExecPushBatch(eventId, auditEvent);
            Task.WaitAll(tasks);
        }

        private async Task PushAsync(Guid eventId, AuditEvent auditEvent)
        {
            var tasks = ExecPushBatch(eventId, auditEvent);
            await Task.WhenAll(tasks);
        }

        private Task[] ExecPushBatch(Guid eventId, AuditEvent auditEvent)
        {
            auditEvent.CustomFields[RedisEventIdField] = eventId;
            var tasks = new List<Task>();
            var key = GetKey(auditEvent);
            var value = GetValue(auditEvent);
            var batch = GetDatabase(auditEvent).CreateBatch();
            tasks.Add(batch.ListLeftPushAsync(key, value));
            if (MaxLength > 0)
            {
                tasks.Add(batch.ListTrimAsync(key, 0, MaxLength - 1));
            }
            if (TimeToLive.HasValue)
            {
                tasks.Add(batch.KeyExpireAsync(key, TimeToLive));
            }
            OnBatchExecuting(batch, tasks, auditEvent);
            batch.Execute();
            return tasks.ToArray();
        }

    }
}