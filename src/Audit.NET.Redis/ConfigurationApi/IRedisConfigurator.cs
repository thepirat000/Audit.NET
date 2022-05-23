using System;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis data provider
    /// </summary>
    public interface IRedisConfigurator 
    {
        /// <summary>
        /// Specifies the redis connection string.
        /// https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </summary>
        /// <param name="connectionString">The redis connection string.</param>
        IRedisConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the redis connection configuration options.
        /// https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </summary>
        /// <param name="configuration">The redis configuration options.</param>
        IRedisConfigurator ConfigurationOptions(ConfigurationOptions configuration);
        /// <summary>
        /// Specifies a custom serializer to store/send the data on/to the redis server.
        /// Default is the audit event serialized as JSon encoded as UTF-8.
        /// </summary>
        /// <param name="serializer">The custom serializer to use.</param>
        IRedisConfigurator Serializer(Func<AuditEvent, byte[]> serializer);
        /// <summary>
        /// Specifies a custom deserializer to retrieve the audit events from the redis server.
        /// Default is the audit event deserialized from JSon UTF-8 encoded.
        /// </summary>
        /// <param name="deserializer">The custom deserializer to use.</param>
        IRedisConfigurator Deserializer(Func<byte[], AuditEvent> deserializer);
        /// <summary>
        /// Specifies that the audit events will be stored as Redis Strings.
        /// </summary>
        /// <param name="config">The redis string configurator.</param>
        void AsString(Action<IRedisStringConfigurator> config);
        /// <summary>
        /// Specifies that the audit events will be stored in a Redis Hash.
        /// </summary>
        /// <param name="config">The redis hash configurator.</param>
        void AsHash(Action<IRedisHashConfigurator> config);
        /// <summary>
        /// Specifies that the audit events will be stored in a Redis List.
        /// </summary>
        /// <param name="config">The redis list configurator.</param>
        void AsList(Action<IRedisListConfigurator> config);
        /// <summary>
        /// Specifies that the audit events will be stored in a Redis Sorted Set.
        /// </summary>
        /// <param name="config">The redis Sorted Set configurator.</param>
        void AsSortedSet(Action<IRedisSortedSetConfigurator> config);
        /// <summary>
        /// Specifies that the audit events will be sent to a Redis PubSub channel.
        /// </summary>
        /// <param name="config">The redis PubSub configurator.</param>
        void AsPubSub(Action<IRedisPubSubConfigurator> config);
        /// <summary>
        /// Specifies that the audit events will be added to a Redis Stream.
        /// </summary>
        /// <param name="config">The redis Stream configurator.</param>
        void AsStream(Action<IRedisStreamConfigurator> config);
    }
}