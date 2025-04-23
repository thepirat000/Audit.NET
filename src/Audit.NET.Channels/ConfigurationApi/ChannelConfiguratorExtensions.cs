using System;
using System.Threading.Channels;

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
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="config">The Channel provider configuration.</param>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator, Action<IChannelProviderConfigurator> config)
        {
            Configuration.DataProvider = new ChannelDataProvider(config);

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in memory in a thread-safe Channel. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="config">The Channel provider configuration.</param>
        /// <param name="channel">The created Channel instance</param>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator, Action<IChannelProviderConfigurator> config, out Channel<AuditEvent> channel)
        {
            var dataProvider = new ChannelDataProvider(config);
            Configuration.DataProvider = dataProvider;
            channel = dataProvider.GetChannel();
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in memory in a thread-safe Channel. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator)
        {
            Configuration.DataProvider = new ChannelDataProvider();

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in memory in a thread-safe Channel. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        /// <param name="channel">The created Channel instance</param>
        public static ICreationPolicyConfigurator UseInMemoryChannelProvider(this IConfigurator configurator, out Channel<AuditEvent> channel)
        {
            var dataProvider = new ChannelDataProvider();
            Configuration.DataProvider = dataProvider;
            channel = dataProvider.GetChannel();
            return new CreationPolicyConfigurator();
        }
    }
}