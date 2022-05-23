using System;
using Audit.Core;
using Audit.Redis.Providers;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    internal class RedisConfigurator : IRedisConfigurator
    {
        internal ConfigurationOptions _configurationOptions;
        internal Func<AuditEvent, byte[]> _serializer;
        internal Func<byte[], AuditEvent> _deserializer;

        public IRedisConfigurator ConnectionString(string connectionString)
        {
            _configurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(connectionString);
            return this;
        }

        public IRedisConfigurator ConfigurationOptions(ConfigurationOptions configuration)
        {
            _configurationOptions = configuration;
            return this;
        }

        public IRedisConfigurator Serializer(Func<AuditEvent, byte[]> serializer)
        {
            _serializer = serializer;
            return this;
        }

        public IRedisConfigurator Deserializer(Func<byte[], AuditEvent> deserializer)
        {
            _deserializer = deserializer;
            return this;
        }

        public void AsString(Action<IRedisStringConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsString(config);
        }

        public void AsHash(Action<IRedisHashConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsHash(config);
        }

        public void AsList(Action<IRedisListConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsList(config);
        }

        public void AsSortedSet(Action<IRedisSortedSetConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsSortedSet(config);
        }

        public void AsPubSub(Action<IRedisPubSubConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsPubSub(config);
        }

        public void AsStream(Action<IRedisStreamConfigurator> config)
        {
            var helper = new RedisDataProviderHelper(_configurationOptions, _serializer, _deserializer);
            Core.Configuration.DataProvider = helper.AsStream(config);
        }
    }
}
