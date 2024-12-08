using Audit.Core;
using Confluent.Kafka;
using System;

namespace Audit.Kafka.Configuration
{
    public class KafkaProviderConfigurator<TKey> : IKafkaProviderConfigurator<TKey>, IKafkaProviderSubConfigurator<TKey>
    {
        internal ProducerConfig _producerConfig;
        internal ISerializer<AuditEvent> _auditEventSerializer;
        internal Func<AuditEvent, TKey> _keySelector;
        internal ISerializer<TKey> _keySerializer;
        internal Setting<int?> _partition;
        internal Setting<string> _topic;
        internal Action<ProducerBuilder<TKey, AuditEvent>> _producerBuilderAction;
        internal Action<DeliveryResult<TKey, AuditEvent>> _resultHandler;
        internal Func<AuditEvent, Headers> _headersSelector;

        public IKafkaProviderSubConfigurator<TKey> AuditEventSerializer(ISerializer<AuditEvent> auditEventSerializer)
        {
            _auditEventSerializer = auditEventSerializer;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> KeySelector(Func<AuditEvent, TKey> keySelector)
        {
            _keySelector = keySelector;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> KeySerializer(ISerializer<TKey> keySerializer)
        {
            _keySerializer = keySerializer;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> Partition(int? partition)
        {
            _partition = partition;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> PartitionSelector(Func<AuditEvent, int?> partitionSelector)
        {
            _partition = partitionSelector;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> ProducerBuilderAction(Action<ProducerBuilder<TKey, AuditEvent>> producerBuilderAction)
        {
            _producerBuilderAction = producerBuilderAction;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> ProducerConfig(ProducerConfig producerConfig)
        {
            _producerConfig = producerConfig;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> ResultHandler(Action<DeliveryResult<TKey, AuditEvent>> resultHandler)
        {
            _resultHandler = resultHandler;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> Topic(string topic)
        {
            _topic = topic;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> TopicSelector(Func<AuditEvent, string> topicSelector)
        {
            _topic = topicSelector;
            return this;
        }

        public IKafkaProviderSubConfigurator<TKey> HeadersSelector(Func<AuditEvent, Headers> headersSelector)
        {
            _headersSelector = headersSelector;
            return this;
        }
    }
}
