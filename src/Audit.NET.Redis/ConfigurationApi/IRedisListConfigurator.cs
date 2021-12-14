using System;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis List handler
    /// </summary>
    public interface IRedisListConfigurator 
    {
        /// <summary>
        /// Specifies a function that returns the Redis Key to use.
        /// </summary>
        /// <param name="keyBuilder">A function that returns the Redis Key</param>
        IRedisListConfigurator Key(Func<AuditEvent, string> keyBuilder);
        /// <summary>
        /// Specifies the Redis Key to use.
        /// </summary>
        /// <param name="key">The Redis Key</param>
        IRedisListConfigurator Key(string key);
        /// <summary>
        /// Specifies the Time To Live for the entire Redis Key. Default is no TTL.
        /// </summary>
        /// <param name="timeToLive">The Time to Live</param>
        IRedisListConfigurator TimeToLive(TimeSpan? timeToLive);
        /// <summary>
        /// Specifies the maximum quantity of events that the list will store. Older elements will be deleted. Default is 0 for no-limit.
        /// </summary>
        /// <param name="maxLength">the maximum quantity of events that the list will store</param>
        IRedisListConfigurator MaxLength(long maxLength);
        /// <summary>
        /// Specifies the redis database ID to use.
        /// </summary>
        /// <param name="dbIndex">Database ID to use</param>
        IRedisListConfigurator Database(int dbIndex);
        /// <summary>
        /// Specifies a function that returns the redis database ID to use.
        /// </summary>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use</param>
        IRedisListConfigurator Database(Func<AuditEvent, int> dbIndexBuilder);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisListConfigurator AttachTask(Func<IBatch, Task> task);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisListConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task);
    }
}