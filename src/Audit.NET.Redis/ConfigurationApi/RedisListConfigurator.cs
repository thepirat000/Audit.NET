using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    internal class RedisListConfigurator : IRedisListConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, int> _dbIndexBuilder;
        internal long _maxLength;
        internal List<Func<IBatch, AuditEvent, Task>> _extraTasks = new List<Func<IBatch, AuditEvent, Task>>();

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

        public IRedisListConfigurator AttachTask(Func<IBatch, Task> task)
        {
            _extraTasks.Add((batch, _) => task.Invoke(batch));
            return this;
        }

        public IRedisListConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task)
        {
            _extraTasks.Add(task);
            return this;
        }
    }
}