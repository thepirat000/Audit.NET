using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis String handler
    /// </summary>
    public interface IRedisStringConfigurator 
    {
        /// <summary>
        /// Specifies a function that returns the Redis Key to use.
        /// </summary>
        /// <param name="keyBuilder">A function that returns the Redis Key</param>
        IRedisStringConfigurator Key(Func<AuditEvent, string> keyBuilder);
        /// <summary>
        /// Specifies the Redis Key to use.
        /// </summary>
        /// <param name="key">The Redis Key</param>
        IRedisStringConfigurator Key(string key);
        /// <summary>
        /// Specifies the Time To Live for the entire Redis Key. Default is no TLL.
        /// </summary>
        /// <param name="timeToLive">The Time to Live</param>
        IRedisStringConfigurator TimeToLive(TimeSpan? timeToLive);
    }
}