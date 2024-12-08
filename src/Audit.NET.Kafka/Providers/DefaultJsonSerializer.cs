using Confluent.Kafka;
using System.Text;
using System;

namespace Audit.Kafka.Providers
{
    /// <summary>
    /// Default JSON serializer for Kafka messages using Audit.Core.JsonAdapter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
            isNull ? default : Core.Configuration.JsonAdapter.Deserialize<T>(Encoding.UTF8.GetString(data.ToArray()));

        public byte[] Serialize(T data, SerializationContext context) =>
            Encoding.UTF8.GetBytes(Core.Configuration.JsonAdapter.Serialize(data));
    }
}
