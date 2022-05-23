using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Adds the audit events to a Redis Stream
    /// </summary>
    public class RedisProviderStream : RedisProviderHandler
    {
        protected int? MaxLength { get; set; }
        protected bool UseApproximateMaxLength { get; set; }
        protected string DefaultAuditEventFieldName { get; set; }
        protected Dictionary<string, Func<AuditEvent, RedisValue>> CustomFieldsDictionary { get; set; }

        /// <summary>
        /// Creates new redis provider that uses a Redis Stream to store the events.
        /// </summary>
        /// <param name="configurationOptions">The redis configuration options.
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="maxLength">Maximum quantity of events that the stream will store. Older elements will be deleted. Default is NULL for no-limit.</param>
        /// <param name="useApproximateMaxLength">If true, the "~" argument is used to allow the stream to exceed max length by a small number. This improves performance when removing messages.</param>
        /// <param name="defaultAuditEventFieldName">Specifies the default field name that will contain the AuditEvent JSON representation in the stream entry.</param>
        /// <param name="fields">The fields and their associated values to set in the stream entry. Default is NULL to set a single field named <code>AuditEvent</code> with the JSON representation of the audit event.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderStream(ConfigurationOptions configurationOptions, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer,
            int? maxLength,
            bool useApproximateMaxLength,
            string defaultAuditEventFieldName,
            Dictionary<string, Func<AuditEvent, RedisValue>> fields,
            Func<AuditEvent, int> dbIndexBuilder,
            List<Func<IBatch, AuditEvent, Task>> extraTasks)
            : base(configurationOptions, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        {
            MaxLength = maxLength;
            UseApproximateMaxLength = useApproximateMaxLength;
            DefaultAuditEventFieldName = defaultAuditEventFieldName;
            CustomFieldsDictionary = fields;
        }

        /// <summary>
        /// Creates new redis provider that uses a Redis Stream to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="maxLength">Maximum quantity of events that the stream will store. Older elements will be deleted. Default is NULL for no-limit.</param>
        /// <param name="useApproximateMaxLength">If true, the "~" argument is used to allow the stream to exceed max length by a small number. This improves performance when removing messages.</param>
        /// <param name="defaultAuditEventFieldName">Specifies the default field name that will contain the AuditEvent JSON representation in the stream entry.</param>
        /// <param name="fields">The fields and their associated values to set in the stream entry. Default is NULL to set a single field named <code>AuditEvent</code> with the JSON representation of the audit event.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderStream(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive,
            Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer,
            int? maxLength,
            bool useApproximateMaxLength,
            string defaultAuditEventFieldName,
            Dictionary<string, Func<AuditEvent, RedisValue>> fields,
            Func<AuditEvent, int> dbIndexBuilder,
            List<Func<IBatch, AuditEvent, Task>> extraTasks)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        {
            MaxLength = maxLength;
            UseApproximateMaxLength = useApproximateMaxLength;
            DefaultAuditEventFieldName = defaultAuditEventFieldName;
            CustomFieldsDictionary = fields;
        }

        public override object Insert(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            var subKey = StreamAdd(key, auditEvent);
            return subKey;
        }

        public override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            var subKey = await StreamAddAsync(key, auditEvent);
            return subKey;
        }

        public override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            // Stream values cannot be replaced, only adding is allowed
            StreamAdd(key, auditEvent);
        }

        public override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            // Stream values cannot be replaced, only adding is allowed
            await StreamAddAsync(key, auditEvent);
        }

        private RedisValue StreamAdd(string key, AuditEvent auditEvent)
        {
            var tasks = ExecStreamSetBatch(key, auditEvent);
            Task.WaitAll(tasks);
            return ((Task<RedisValue>) tasks[0]).Result;
        }

        private async Task<RedisValue> StreamAddAsync(string key, AuditEvent auditEvent)
        {
            var tasks = ExecStreamSetBatch(key, auditEvent);
            await Task.WhenAll(tasks);
            return ((Task<RedisValue>)tasks[0]).Result;
        }

        private Task[] ExecStreamSetBatch(string key, AuditEvent auditEvent)
        {
            var db = GetDatabase(auditEvent);
            var batch = db.CreateBatch();
            var tasks = new List<Task>();
            var fieldValues = new List<NameValueEntry>();
            if (DefaultAuditEventFieldName != null)
            {
                fieldValues.Add(new NameValueEntry(DefaultAuditEventFieldName, GetValue(auditEvent)));
            }
            if (CustomFieldsDictionary != null)
            {
                foreach (var field in CustomFieldsDictionary)
                {
                    fieldValues.Add(new NameValueEntry(field.Key, field.Value.Invoke(auditEvent)));
                }
            }
            tasks.Add(batch.StreamAddAsync(key, fieldValues.ToArray(), null, MaxLength, UseApproximateMaxLength));
            OnBatchExecuting(batch, tasks, auditEvent);
            batch.Execute();
            return tasks.ToArray();
        }

        public override T Get<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            var entries = db.StreamRange(key, subKey.ToString(), subKey.ToString(), 1);
            foreach (var entry in entries)
            {
                foreach (var value in entry.Values)
                {
                    if (value.Name.Equals(DefaultAuditEventFieldName))
                    {
                        return FromValue<T>(value.Value);
                    }
                }
            }

            return default;
        }

        public override async Task<T> GetAsync<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            var entries = await db.StreamRangeAsync(key, subKey.ToString(), subKey.ToString(), 1);
            foreach (var entry in entries)
            {
                foreach (var value in entry.Values)
                {
                    if (value.Name.Equals(DefaultAuditEventFieldName))
                    {
                        return FromValue<T>(value.Value);
                    }
                }
            }

            return default;
        }
    }
}