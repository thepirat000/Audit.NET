using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    internal class RedisHashConfigurator : IRedisHashConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, int> _dbIndexBuilder;
        internal Func<AuditEvent, string> _fieldBuilder;
        internal List<Func<IBatch, AuditEvent, Task>> _extraTasks = new List<Func<IBatch, AuditEvent, Task>>();

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

        public IRedisHashConfigurator Database(int dbIndex)
        {
            _dbIndexBuilder = _ => dbIndex;
            return this;
        }

        public IRedisHashConfigurator Database(Func<AuditEvent, int> dbIndexBuilder)
        {
            _dbIndexBuilder = dbIndexBuilder;
            return this;
        }

        public IRedisHashConfigurator HashField(Func<AuditEvent, string> fieldBuilder)
        {
            _fieldBuilder = fieldBuilder;
            return this;
        }

        public IRedisHashConfigurator AttachTask(Func<IBatch, Task> task)
        {
            _extraTasks.Add((batch, _) => task.Invoke(batch));
            return this;
        }

        public IRedisHashConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task)
        {
            _extraTasks.Add(task);
            return this;
        }
    }
}