using System;
using StackExchange.Redis;
using Audit.Core;
using Audit.Redis.Configuration;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Provides a fluent API to create Redis Data Providers
    /// </summary>
    public class RedisDataProviderHelper
    {
        internal ConfigurationOptions _configurationOptions;
        internal Func<AuditEvent, byte[]> _serializer;
        internal Func<byte[], AuditEvent> _deserializer;

        /// <summary>
        /// Creates a new helper instance with the given connection string and serializer
        /// </summary>
        /// <param name="connectionString">The redis connection string to be used</param>
        /// <param name="serializer">Specifies a custom serializer to store/send the data on/to the redis server</param>
        /// <param name="deserializer">Specifies a custom deserializer to retrieve events from the redis server</param>
        public RedisDataProviderHelper(string connectionString, Func<AuditEvent, byte[]> serializer, Func<byte[], AuditEvent> deserializer)
        {
            _configurationOptions = ConfigurationOptions.Parse(connectionString);
            _serializer = serializer;
            _deserializer = deserializer;
        }

        /// <summary>
        /// Creates a new helper instance with the given connection configuration and serializer
        /// </summary>
        /// <param name="configurationOptions">The redis connection configuration options to be used</param>
        /// <param name="serializer">Specifies a custom serializer to store/send the data on/to the redis server</param>
        /// <param name="deserializer">Specifies a custom deserializer to retrieve events from the redis server</param>
        public RedisDataProviderHelper(ConfigurationOptions configurationOptions, Func<AuditEvent, byte[]> serializer, Func<byte[], AuditEvent> deserializer)
        {
            _configurationOptions = configurationOptions;
            _serializer = serializer;
            _deserializer = deserializer;
        }

        /// <summary>
        /// Returns a data provider that stores the events as Redis Strings.
        /// </summary>
        /// <param name="config">The redis string configuration.</param>
        public IAuditDataProvider AsString(Action<IRedisStringConfigurator> config)
        {
            var strConfig = new RedisStringConfigurator();
            config.Invoke(strConfig);
            return new RedisDataProvider(new RedisProviderString(
                _configurationOptions, strConfig._keyBuilder, strConfig._timeToLive, _serializer, _deserializer, strConfig._dbIndexBuilder, strConfig._extraTasks));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Hashes.
        /// </summary>
        /// <param name="config">The redis hash configuration.</param>
        public IAuditDataProvider AsHash(Action<IRedisHashConfigurator> config)
        {
            var hashConfig = new RedisHashConfigurator();
            config.Invoke(hashConfig);
            return new RedisDataProvider(new RedisProviderHash(
                _configurationOptions, hashConfig._keyBuilder, hashConfig._timeToLive, _serializer, _deserializer,
                hashConfig._fieldBuilder, hashConfig._dbIndexBuilder, hashConfig._extraTasks));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Lists.
        /// </summary>
        /// <param name="config">The redis list configuration.</param>
        public IAuditDataProvider AsList(Action<IRedisListConfigurator> config)
        {
            var listConfig = new RedisListConfigurator();
            config.Invoke(listConfig);
            return new RedisDataProvider(new RedisProviderList(
                _configurationOptions, listConfig._keyBuilder, listConfig._timeToLive, _serializer, _deserializer,
                listConfig._maxLength, listConfig._dbIndexBuilder, listConfig._extraTasks));
        }

        /// <summary>
        /// Returns a data provider that stores the events in Redis Sorted Sets.
        /// </summary>
        /// <param name="config">The redis sorted set configuration.</param>
        public IAuditDataProvider AsSortedSet(Action<IRedisSortedSetConfigurator> config)
        {
            var ssConfig = new RedisSortedSetConfigurator();
            config.Invoke(ssConfig);
            return new RedisDataProvider(new RedisProviderSortedSet(
                _configurationOptions, ssConfig._keyBuilder, ssConfig._timeToLive, _serializer, _deserializer,
                ssConfig._scoreBuilder, ssConfig._maxScoreBuilder, ssConfig._maxScoreExclusive, ssConfig._minScoreBuilder, ssConfig._minScoreExclusive,
                ssConfig._maxRankBuilder, ssConfig._dbIndexBuilder, ssConfig._extraTasks));
        }

        /// <summary>
        /// Returns a data provider that sends the events to a Redis PubSub channel.
        /// </summary>
        /// <param name="config">The redis pub-sub configuration.</param>
        public IAuditDataProvider AsPubSub(Action<IRedisPubSubConfigurator> config)
        {
            var pubConfig = new RedisPubSubConfigurator();
            config.Invoke(pubConfig);
            return new RedisDataProvider(new RedisProviderPubSub(_configurationOptions, _serializer, pubConfig._channelBuilder));
        }

        /// <summary>
        /// Returns a data provider that adds the events to Redis Streams.
        /// </summary>
        /// <param name="config">The redis stream configuration.</param>
        public IAuditDataProvider AsStream(Action<IRedisStreamConfigurator> config)
        {
            var streamConfig = new RedisStreamConfigurator();
            config.Invoke(streamConfig);
            return new RedisDataProvider(new RedisProviderStream(
                _configurationOptions, streamConfig._keyBuilder, null, _serializer, _deserializer,
                streamConfig._maxLength, streamConfig._useApproximateMaxLength, streamConfig._defaultAuditEventFieldName,
                streamConfig._customFieldsDictionary, streamConfig._dbIndexBuilder, streamConfig._extraTasks));
        }
    }
}