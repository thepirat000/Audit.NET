using System;
using Audit.Core;

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
        /// Specifies the Time To Live for the entire Redis Key. Default is no TLL.
        /// </summary>
        /// <param name="timeToLive">The Time to Live</param>
        IRedisListConfigurator TimeToLive(TimeSpan? timeToLive);
        /// <summary>
        /// Specifies the maximum quantity of events that the list will store. Older elements will be deleted. Default is 0 for no-limit.
        /// </summary>
        /// <param name="maxLength">the maximum quantity of events that the list will store</param>
        IRedisListConfigurator MaxLength(long maxLength);
    }
}