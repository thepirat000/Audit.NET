using System;
using Audit.Channels.Configuration;
using Audit.Channels.Providers;
using Audit.Core.ConfigurationApi;

namespace Audit.Core
{
    public static class ChannelConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in memory in a thread-safe Channel. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator, Action<IChannelProviderConfigurator> config)
        {
            Configuration.DataProvider = new ChannelDataProvider(config);

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in memory in a thread-safe Channel. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator)
        {
            Configuration.DataProvider = new ChannelDataProvider();

            return new CreationPolicyConfigurator();
        }
    }
}