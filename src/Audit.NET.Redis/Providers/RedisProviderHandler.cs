using System;
using System.Text;
using System.Threading;
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

        protected RedisProviderHandler(string connectionString, Func<AuditEvent, string> keyBuilder, TimeSpan? timeToLive, Func<AuditEvent, byte[]> serializer)
        {
            Context = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString), LazyThreadSafetyMode.ExecutionAndPublication);
            ConnectionString = connectionString;
            KeyBuilder = keyBuilder;
            TimeToLive = timeToLive;
            Serializer = serializer ?? (ev => Encoding.UTF8.GetBytes(ev.ToJson()));
        }

        internal abstract object Insert(AuditEvent auditEvent);

        internal abstract void Replace(object eventId, AuditEvent auditEvent);

        internal IDatabase GetDatabase()
        {
            return Context.Value.GetDatabase();
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
    }
}