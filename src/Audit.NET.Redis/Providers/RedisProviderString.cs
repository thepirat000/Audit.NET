using System;
using System.Threading.Tasks;
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
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        public RedisProviderString(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer)
        { }

        internal override object Insert(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            StringSet(key, auditEvent);
            return key;
        }

        internal override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var key = GetKey(auditEvent);
            await StringSetAsync(key, auditEvent);
            return key;
        }

        internal override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            StringSet(key, auditEvent);
        }

        internal override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            await StringSetAsync(key, auditEvent);
        }

        internal override T Get<T>(string key, object subKey)
        {
            return StringGet<T>(key);
        }

        internal override async Task<T> GetAsync<T>(string key, object subKey)
        {
            return await StringGetAsync<T>(key);
        }

        private void StringSet(string key, AuditEvent auditEvent)
        {
            var db = GetDatabase();
            var value = GetValue(auditEvent);
            db.StringSet(key, value, TimeToLive);
        }

        private async Task StringSetAsync(string key, AuditEvent auditEvent)
        {
            var db = GetDatabase();
            var value = GetValue(auditEvent);
            await db.StringSetAsync(key, value, TimeToLive);
        }

        private T StringGet<T>(string key) where T : AuditEvent
        {
            var db = GetDatabase();
            var value = db.StringGet(key);
            return FromValue<T>(value);
        }

        private async Task<T> StringGetAsync<T>(string key) where T : AuditEvent
        {
            var db = GetDatabase();
            var value = await db.StringGetAsync(key);
            return FromValue<T>(value);
        }
    }
}