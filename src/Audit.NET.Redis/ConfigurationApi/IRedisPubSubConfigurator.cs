using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis PubSub handler
    /// </summary>
    public interface IRedisPubSubConfigurator
    {
        /// <summary>
        /// Specifies a function that returns the Redis PubSub Channel to use.
        /// </summary>
        /// <param name="channelBuilder">A function that returns the Redis PubSub Channel to use</param>
        IRedisPubSubConfigurator Channel(Func<AuditEvent, string> channelBuilder);
        /// <summary>
        /// Specifies the Redis PubSub Channel to use.
        /// </summary>
        /// <param name="channel">The Redis PubSub Channel to use</param>
        IRedisPubSubConfigurator Channel(string channel);
    }
}