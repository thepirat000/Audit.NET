using System;
using Audit.Core;
using Audit.Redis.Configuration;
using Audit.Redis.Providers;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Provides a fluent API to create Redis Data Providers
    /// </summary>
    public class RedisDataProviderHelper
    {
        internal string _connectionString;
        internal Func<AuditEvent, byte[]> _serializer;
        internal Func<byte[], AuditEvent> _deserializer;

        /// <summary>
        /// Creates a new helper instance with the given connection string
        /// </summary>
        /// <param name="connectionString">The redis connection string to be used</param>
        public RedisDataProviderHelper(string connectionString)
            : this(connectionString, null, null) { }

        /// <summary>
        /// Creates a new helper instance with the given connection string and serializer
        /// </summary>
        /// <param name="connectionString">The redis connection string to be used</param>
        /// <param name="serializer">Specifies a custom serializer to store/send the data on/to the redis server</param>
        /// <param name="deserializer">Specifies a custom deserializer to retrieve events from the redis server</param>
        public RedisDataProviderHelper(string connectionString, Func<AuditEvent, byte[]> serializer, Func<byte[], AuditEvent> deserializer)
        {
            _connectionString = connectionString;
            _serializer = serializer;
            _deserializer = deserializer;
        }

        /// <summary>
        /// Returns a data provider that stores the events as Redis Strings.
        /// </summary>
        /// <param name="config">The redis string configuration.</param>
        public AuditDataProvider AsString(Action<IRedisStringConfigurator> config)
        {
            var strConfig = new RedisStringConfigurator();
            config.Invoke(strConfig);
            return new RedisDataProvider(new RedisProviderString(
                _connectionString, strConfig._keyBuilder, strConfig._timeToLive, _serializer, _deserializer));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Hashes.
        /// </summary>
        /// <param name="config">The redis hash configuration.</param>
        public AuditDataProvider AsHash(Action<IRedisHashConfigurator> config)
        {
            var hashConfig = new RedisHashConfigurator();
            config.Invoke(hashConfig);
            return new RedisDataProvider(new RedisProviderHash(
                _connectionString, hashConfig._keyBuilder, hashConfig._timeToLive, _serializer, _deserializer,
                hashConfig._fieldBuilder));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Lists.
        /// </summary>
        /// <param name="config">The redis list configuration.</param>
        public AuditDataProvider AsList(Action<IRedisListConfigurator> config)
        {
            var listConfig = new RedisListConfigurator();
            config.Invoke(listConfig);
            return new RedisDataProvider(new RedisProviderList(
                _connectionString, listConfig._keyBuilder, listConfig._timeToLive, _serializer, _deserializer,
                listConfig._maxLength));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Sorted Sets.
        /// </summary>
        /// <param name="config">The redis sorted set configuration.</param>
        public AuditDataProvider AsSortedSet(Action<IRedisSortedSetConfigurator> config)
        {
            var ssConfig = new RedisSortedSetConfigurator();
            config.Invoke(ssConfig);
            return new RedisDataProvider(new RedisProviderSortedSet(
                _connectionString, ssConfig._keyBuilder, ssConfig._timeToLive, _serializer, _deserializer,
                ssConfig._scoreBuilder, ssConfig._maxScoreBuilder, ssConfig._maxScoreExclusive, ssConfig._minScoreBuilder, ssConfig._minScoreExclusive,
                ssConfig._maxRankBuilder));
        }

        /// <summary>
        /// Returns a data provider that sends the events to a Redis PubSub channel.
        /// </summary>
        /// <param name="config">The redis pub-sub configuration.</param>
        public AuditDataProvider AsPubSub(Action<IRedisPubSubConfigurator> config)
        {
            var pubConfig = new RedisPubSubConfigurator();
            config.Invoke(pubConfig);
            return new RedisDataProvider(new RedisProviderPubSub(_connectionString, _serializer, pubConfig._channelBuilder));
        }
    }
}