using System;
using System.Collections.Generic;
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

        protected List<Func<IBatch, AuditEvent, Task>> ExtraTasks { get; set; }

        protected RedisProviderHandler(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer, Func<AuditEvent, int> dbIndexBuilder, List<Func<IBatch, AuditEvent, Task>> extraTasks) 
            : this(ConfigurationOptions.Parse(connectionString), keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        {
        }

        protected RedisProviderHandler(ConfigurationOptions configurationOptions, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer, Func<AuditEvent, int> dbIndexBuilder, List<Func<IBatch, AuditEvent, Task>> extraTasks)
        {
            Context = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(configurationOptions), LazyThreadSafetyMode.ExecutionAndPublication);
            KeyBuilder = keyBuilder;
            TimeToLive = timeToLive;
            Serializer = serializer ?? (ev => Encoding.UTF8.GetBytes(ev.ToJson()));
            Deserializer = deserializer;
            DbIndexBuilder = dbIndexBuilder;
            ExtraTasks = extraTasks ?? new List<Func<IBatch, AuditEvent, Task>>();
        }

        public abstract object Insert(AuditEvent auditEvent);

        public abstract Task<object> InsertAsync(AuditEvent auditEvent);

        public abstract void Replace(string key, object subKey, AuditEvent auditEvent);

        public abstract Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent);

        public virtual T Get<T>(string key, object subKey) where T : AuditEvent
        {
            throw new NotImplementedException($"Events retrieval is not supported by {GetType().Name}");
        }

        public virtual Task<T> GetAsync<T>(string key, object subKey) where T : AuditEvent
        {
            throw new NotImplementedException($"Events retrieval is not supported by {GetType().Name}");
        }

        public IDatabase GetDatabase(AuditEvent auditEvent)
        {
            return Context.Value.GetDatabase(DbIndexBuilder?.Invoke(auditEvent) ?? -1);
        }

        public string GetKey(AuditEvent auditEvent)
        {
            if (KeyBuilder == null)
            {
                throw new ArgumentException("The redis key was not provided.");
            }
            return KeyBuilder.Invoke(auditEvent);
        }

        public RedisValue GetValue(AuditEvent auditEvent)
        {
            return (RedisValue)Serializer.Invoke(auditEvent);
        }

        public T FromValue<T>(RedisValue value) where T : AuditEvent
        {
            if (Deserializer == null)
            {
                return AuditEvent.FromJson<T>(value);
            }
            return Deserializer.Invoke(value) as T;
        }

        protected virtual void OnBatchExecuting(IBatch batch, List<Task> tasks, AuditEvent auditEvent)
        {
            foreach (var task in ExtraTasks)
            {
                tasks.Add(task.Invoke(batch, auditEvent));
            }
        }

    }
}