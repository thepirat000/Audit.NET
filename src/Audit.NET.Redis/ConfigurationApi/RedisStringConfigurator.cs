using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisStringConfigurator : IRedisStringConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;

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

        public IRedisStringConfigurator TimeToLive(TimeSpan? timeToLive)
        {
            _timeToLive = timeToLive;
            return this;
        }
    }
}