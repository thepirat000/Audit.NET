using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Base class for the redis handlers
    /// </summary>
    public abstract class RedisProviderHandler 
    {
        protected const string RedisEventIdField = "RedisEventId";
        protected Lazy<ConnectionMultiplexer> Context { get; set; }
        protected string ConnectionString { get; set; }
        protected Func<AuditEvent, string> KeyBuilder { get; set; }
        protected TimeSpan? TimeToLive { get; set; }
        protected Func<AuditEvent, byte[]> Serializer { get; set; }
        protected Func<byte[], object> Deserializer { get; set; }
        protected Func<AuditEvent, int> DbIndexBuilder { get; set; }


        protected RedisProviderHandler(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer, Func<AuditEvent, int> dbIndexBuilder)
        {
            Context = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString), LazyThreadSafetyMode.ExecutionAndPublication);
            ConnectionString = connectionString;
            KeyBuilder = keyBuilder;
            TimeToLive = timeToLive;
            Serializer = serializer ?? (ev => Encoding.UTF8.GetBytes(ev.ToJson()));
            Deserializer = deserializer;
            DbIndexBuilder = dbIndexBuilder;
        }

        internal abstract object Insert(AuditEvent auditEvent);
        internal abstract Task<object> InsertAsync(AuditEvent auditEvent);
        internal abstract void Replace(string key, object subKey, AuditEvent auditEvent);
        internal abstract Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent);
        internal virtual T Get<T>(string key, object subKey) where T : AuditEvent
        {
            throw new NotImplementedException($"Events retrieval is not supported by {GetType().Name}");
        }
        internal virtual Task<T> GetAsync<T>(string key, object subKey) where T : AuditEvent
        {
            throw new NotImplementedException($"Events retrieval is not supported by {GetType().Name}");
        }

        internal IDatabase GetDatabase(AuditEvent auditEvent)
        {
            return Context.Value.GetDatabase(DbIndexBuilder?.Invoke(auditEvent) ?? -1);
        }

        internal string GetKey(AuditEvent auditEvent)
        {
            if (KeyBuilder == null)
            {
                throw new ArgumentException("The redis key was not provided.");
            }
            return KeyBuilder.Invoke(auditEvent);
        }

        internal RedisValue GetValue(AuditEvent auditEvent)
        {
            return (RedisValue)Serializer.Invoke(auditEvent);
        }

        internal T FromValue<T>(RedisValue value) where T : AuditEvent
        {
            if (Deserializer == null)
            {
                return AuditEvent.FromJson<T>(value);
            }
            return Deserializer.Invoke(value) as T;
        }
    }
}