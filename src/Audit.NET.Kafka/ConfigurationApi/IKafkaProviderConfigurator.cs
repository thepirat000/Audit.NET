using Confluent.Kafka;

namespace Audit.Kafka.Configuration
{
    public interface IKafkaProviderConfigurator<TKey>
    {
        /// <summary>
        /// Sets the Kafka Producer configuration
        /// </summary>
        /// <param name="producerConfig">The producer configuration.</param>
        IKafkaProviderSubConfigurator<TKey> ProducerConfig(ProducerConfig producerConfig);
    }
}
