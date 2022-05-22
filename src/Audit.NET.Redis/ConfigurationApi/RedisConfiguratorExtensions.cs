using System;
using Audit.Core.ConfigurationApi;
using Audit.Redis.Configuration;

namespace Audit.Core
{
    public static class RedisConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a Redis database.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="redisConfig">The redis configuration. This parameter is mandatory.</param>
        public static ICreationPolicyConfigurator UseRedis(this IConfigurator configurator, Action<IRedisConfigurator> redisConfig)
        {
            var rc = new RedisConfigurator();
            redisConfig.Invoke(rc);
            return new CreationPolicyConfigurator();
        }
    }
}
