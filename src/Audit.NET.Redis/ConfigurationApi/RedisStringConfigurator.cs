using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    internal class RedisStringConfigurator : IRedisStringConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, int> _dbIndexBuilder;
        internal List<Func<IBatch, AuditEvent, Task>> _extraTasks = new List<Func<IBatch, AuditEvent, Task>>();

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

        public IRedisStringConfigurator AttachTask(Func<IBatch, Task> task)
        {
            _extraTasks.Add((batch, _) => task.Invoke(batch));
            return this;
        }

        public IRedisStringConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task)
        {
            _extraTasks.Add(task);
            return this;
        }
    }
}