using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisListConfigurator : IRedisListConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, int> _dbIndexBuilder;
        internal long _maxLength;

        public IRedisListConfigurator Key(Func<AuditEvent, string> keyBuilder)
        {
            _keyBuilder = keyBuilder;
            return this;
        }

        public IRedisListConfigurator Key(string key)
        {
            _keyBuilder = ev => key;
            return this;
        }

        public IRedisListConfigurator TimeToLive(TimeSpan? timeToLive)
        {
            _timeToLive = timeToLive;
            return this;
        }

        public IRedisListConfigurator Database(int dbIndex)
        {
            _dbIndexBuilder = _ => dbIndex;
            return this;
        }

        public IRedisListConfigurator Database(Func<AuditEvent, int> dbIndexBuilder)
        {
            _dbIndexBuilder = dbIndexBuilder;
            return this;
        }

        public IRedisListConfigurator MaxLength(long maxLength)
        {
            _maxLength = maxLength;
            return this;
        }
    }
}