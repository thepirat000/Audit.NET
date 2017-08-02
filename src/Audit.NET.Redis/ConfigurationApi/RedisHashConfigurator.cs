using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisHashConfigurator : IRedisHashConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, string> _fieldBuilder;

        public IRedisHashConfigurator Key(Func<AuditEvent, string> keyBuilder)
        {
            _keyBuilder = keyBuilder;
            return this;
        }

        public IRedisHashConfigurator Key(string key)
        {
            _keyBuilder = ev => key;
            return this;
        }

        public IRedisHashConfigurator TimeToLive(TimeSpan? timeToLive)
        {
            _timeToLive = timeToLive;
            return this;
        }

        public IRedisHashConfigurator HashField(Func<AuditEvent, string> fieldBuilder)
        {
            _fieldBuilder = fieldBuilder;
            return this;
        }
    }
}