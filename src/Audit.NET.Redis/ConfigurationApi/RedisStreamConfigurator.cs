using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    internal class RedisStreamConfigurator : IRedisStreamConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal Func<AuditEvent, int> _dbIndexBuilder;
        internal int? _maxLength;
        internal bool _useApproximateMaxLength;
        internal string _defaultAuditEventFieldName = nameof(AuditEvent);
        internal Dictionary<string, Func<AuditEvent, RedisValue>> _customFieldsDictionary;

        internal List<Func<IBatch, AuditEvent, Task>> _extraTasks = new List<Func<IBatch, AuditEvent, Task>>();

        public IRedisStreamConfigurator Key(Func<AuditEvent, string> keyBuilder)
        {
            _keyBuilder = keyBuilder;
            return this;
        }

        public IRedisStreamConfigurator Key(string key)
        {
            _keyBuilder = ev => key;
            return this;
        }

        public IRedisStreamConfigurator MaxLength(int? maxLength, bool useApproximateMaxLength = false)
        {
            _maxLength = maxLength;
            _useApproximateMaxLength = useApproximateMaxLength;
            return this;
        }

        public IRedisStreamConfigurator WithCustomField(string fieldName, Func<AuditEvent, RedisValue> fieldValueGetter)
        {
            if (_customFieldsDictionary == null)
            {
                _customFieldsDictionary = new Dictionary<string, Func<AuditEvent, RedisValue>>();
            }
            _customFieldsDictionary[fieldName] = fieldValueGetter;
            return this;
        }

        public IRedisStreamConfigurator DefaultAuditEventFieldName(string fieldName)
        {
            _defaultAuditEventFieldName = fieldName;
            return this;
        }

        public IRedisStreamConfigurator Database(int dbIndex)
        {
            _dbIndexBuilder = _ => dbIndex;
            return this;
        }

        public IRedisStreamConfigurator Database(Func<AuditEvent, int> dbIndexBuilder)
        {
            _dbIndexBuilder = dbIndexBuilder;
            return this;
        }

        public IRedisStreamConfigurator AttachTask(Func<IBatch, Task> task)
        {
            _extraTasks.Add((batch, _) => task.Invoke(batch));
            return this;
        }

        public IRedisStreamConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task)
        {
            _extraTasks.Add(task);
            return this;
        }
    }
}