using System;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis Hash handler
    /// </summary>
    public interface IRedisHashConfigurator
    {
        /// <summary>
        /// Specifies a function that returns the Redis Key to use.
        /// </summary>
        /// <param name="keyBuilder">A function that returns the Redis Key</param>
        IRedisHashConfigurator Key(Func<AuditEvent, string> keyBuilder);
        /// <summary>
        /// Specifies the Redis Key to use.
        /// </summary>
        /// <param name="key">The Redis Key</param>
        IRedisHashConfigurator Key(string key);
        /// <summary>
        /// Specifies the Time To Live for the Redis Key. Default is no TTL.
        /// </summary>
        /// <param name="timeToLive">The Time to Live</param>
        IRedisHashConfigurator TimeToLive(TimeSpan? timeToLive);
        /// <summary>
        /// Specifies a function that returns the hash field to use.
        /// </summary>
        /// <param name="fieldBuilder">A function that returns the hash field to use</param>
        IRedisHashConfigurator HashField(Func<AuditEvent, string> fieldBuilder);
        /// <summary>
        /// Specifies the redis database ID to use.
        /// </summary>
        /// <param name="dbIndex">Database ID to use</param>
        IRedisHashConfigurator Database(int dbIndex);
        /// <summary>
        /// Specifies a function that returns the redis database ID to use.
        /// </summary>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use</param>
        IRedisHashConfigurator Database(Func<AuditEvent, int> dbIndexBuilder);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisHashConfigurator AttachTask(Func<IBatch, Task> task);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisHashConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task);
    }
}