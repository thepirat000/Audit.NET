using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisStringConfigurator : IRedisStringConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, int> _dbIndexBuilder;

        public IRedisStringConfigurator Key(string key)
        {
            _keyBuilder = ev => key;
            return this;
        }

        public IRedisStringConfigurator Key(Func<AuditEvent, string> keyBuilder)
        {
            _keyBuilder = keyBuilder;
            return this;
        }

        public IRedisStringConfigurator Database(int dbIndex)
        {
            _dbIndexBuilder = _ => dbIndex;
            return this;
        }

        public IRedisStringConfigurator Database(Func<AuditEvent, int> dbIndexBuilder)
        {
            _dbIndexBuilder = dbIndexBuilder;
            return this;
        }

        public IRedisStringConfigurator TimeToLive(TimeSpan? timeToLive)
        {
            _timeToLive = timeToLive;
            return this;
        }
    }
}