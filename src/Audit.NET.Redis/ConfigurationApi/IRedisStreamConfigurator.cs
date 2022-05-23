using System;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis Stream handler
    /// </summary>
    public interface IRedisStreamConfigurator 
    {
        /// <summary>
        /// Specifies a function that returns the Redis Key to use.
        /// </summary>
        /// <param name="keyBuilder">A function that returns the Redis Key</param>
        IRedisStreamConfigurator Key(Func<AuditEvent, string> keyBuilder);
        /// <summary>
        /// Specifies the Redis Key to use.
        /// </summary>
        /// <param name="key">The Redis Key</param>
        IRedisStreamConfigurator Key(string key);
        /// <summary>
        /// Specifies the maximum quantity of events that the stream will store. Older elements will be deleted. Default is NULL for no-limit.
        /// </summary>
        /// <param name="maxLength">the maximum quantity of events that the stream will store</param>
        /// <param name="useApproximateMaxLength">If true, allows the stream to exceed max length by a small number. This improves performance when removing messages. Default is false.</param>
        IRedisStreamConfigurator MaxLength(int? maxLength, bool useApproximateMaxLength = false);
        /// <summary>
        /// Specifies a custom field to be stored on the stream entry.
        /// By default, only the field named "AuditEvent" is stored, containing the JSON representation of the Audit Event.
        /// </summary>
        /// <param name="fieldName">Name of the field to add to the stream entry.</param>
        /// <param name="fieldValueGetter">The field value getter.</param>
        IRedisStreamConfigurator WithCustomField(string fieldName, Func<AuditEvent, RedisValue> fieldValueGetter);
        /// <summary>
        /// Specifies the default field name that will contain the AuditEvent JSON representation in the stream entry.
        /// Can be set to NULL to avoid storing the AuditEvent, so only the Custom Fields will be stored.
        /// Default is "AuditEvent".
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        IRedisStreamConfigurator DefaultAuditEventFieldName(string fieldName);
        /// <summary>
        /// Specifies the redis database ID to use.
        /// </summary>
        /// <param name="dbIndex">Database ID to use</param>
        IRedisStreamConfigurator Database(int dbIndex);
        /// <summary>
        /// Specifies a function that returns the redis database ID to use.
        /// </summary>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use</param>
        IRedisStreamConfigurator Database(Func<AuditEvent, int> dbIndexBuilder);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisStreamConfigurator AttachTask(Func<IBatch, Task> task);
        /// <summary>
        /// Attach an extra redis command to the execution
        /// </summary>
        /// <param name="task">Task to add to the batch</param>
        IRedisStreamConfigurator AttachTask(Func<IBatch, AuditEvent, Task> task);
    }
}