using Audit.Core;
using Confluent.Kafka;
using System;

namespace Audit.Kafka.Configuration
{
    public interface IKafkaProviderSubConfigurator<TKey>
    {
        /// <summary>
        /// Set the Kafka Topic to be used. Default is "audit-topic".
        /// </summary>
        /// <param name="topic">The topic name.</param>
        IKafkaProviderSubConfigurator<TKey> Topic(string topic);
        /// <summary>
        /// Set the Kafka Topic selector to be used. Default is "audit-topic".
        /// </summary>
        /// <param name="topicSelector">The topic selector function that returns the topic name to use for the given audit event.</param>
        IKafkaProviderSubConfigurator<TKey> TopicSelector(Func<AuditEvent, string> topicSelector);
        /// <summary>
        /// Set a specific Partition to be used. Default or NULL means any partition
        /// </summary>
        /// <param name="partition">The partition.</param>
        IKafkaProviderSubConfigurator<TKey> Partition(int? partition);
        /// <summary>
        /// Set the Partition selector to be used as a function of the audit event. Default or NULL means any partition
        /// </summary>
        /// <param name="partitionSelector">The partition selector. Default or NULL means any partition</param>
        IKafkaProviderSubConfigurator<TKey> PartitionSelector(Func<AuditEvent, int?> partitionSelector);
        /// <summary>
        /// Sets the Key selector. Optional to use keyed messages. Return the key to be used for a given audit event.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        IKafkaProviderSubConfigurator<TKey> KeySelector(Func<AuditEvent, TKey> keySelector);
        /// <summary>
        /// Sets the Headers selector. Optional to use message headers. Configure the message headers to be used for a given audit event.
        /// </summary>
        /// <param name="headersSelector"></param>
        /// <returns></returns>
        IKafkaProviderSubConfigurator<TKey> HeadersSelector(Func<AuditEvent, Headers> headersSelector);
        /// <summary>
        /// Sets the Key serializer. Optional when using keyed messages and a custom serializer for the key is needed.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        IKafkaProviderSubConfigurator<TKey> KeySerializer(ISerializer<TKey> keySerializer);
        /// <summary>
        /// Sets a custom AuditEvent serializer. By default the audit event is JSON serialized + UTF8 encoded.
        /// </summary>
        /// <param name="auditEventSerializer">The audit event serializer.</param>
        IKafkaProviderSubConfigurator<TKey> AuditEventSerializer(ISerializer<AuditEvent> auditEventSerializer);
        /// <summary>
        /// Sets a result handler. An action to be called for each kafka response.
        /// </summary>
        /// <param name="resultHandler">The result handler.</param>
        IKafkaProviderSubConfigurator<TKey> ResultHandler(Action<DeliveryResult<TKey, AuditEvent>> resultHandler);
        /// <summary>
        /// Sets the producer builder action. An action to be called before building the producer to provide custom configuration.
        /// </summary>
        /// <param name="producerBuilderAction">The producer builder action.</param>
        IKafkaProviderSubConfigurator<TKey> ProducerBuilderAction(Action<ProducerBuilder<TKey, AuditEvent>> producerBuilderAction);
    }
}
