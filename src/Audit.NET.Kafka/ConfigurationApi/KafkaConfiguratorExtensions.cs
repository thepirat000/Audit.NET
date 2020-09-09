using Audit.Core.ConfigurationApi;
using Audit.Kafka.Configuration;
using Audit.Kafka.Providers;
using Confluent.Kafka;
using System;

namespace Audit.Core
{
    public static class KafkaConfiguratorExtensions
    {
        /// <summary>
        /// Send the audit events to an Apache Kafka server using keyed messages.
        /// </summary>
        /// <param name="config">The Kafka server provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseKafka<TKey>(this IConfigurator configurator, Action<IKafkaProviderConfigurator<TKey>> config)
        {
            Configuration.DataProvider = new KafkaDataProvider<TKey>(config);
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Send the audit events to an Apache Kafka server.
        /// </summary>
        /// <param name="config">The Kafka server provider configuration.</param>
        /// <param name="configurator">The Audit.NET configurator object.</param>
        public static ICreationPolicyConfigurator UseKafka(this IConfigurator configurator, Action<IKafkaProviderConfigurator<Null>> config)
        {
            Configuration.DataProvider = new KafkaDataProvider(config);
            return new CreationPolicyConfigurator();
        }

    }
}
