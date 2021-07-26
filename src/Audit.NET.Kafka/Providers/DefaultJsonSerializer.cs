using Confluent.Kafka;
using System.Text;
using System;

namespace Audit.Kafka.Providers
{
    public class DefaultJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
            isNull ? default : Core.Configuration.JsonAdapter.Deserialize<T>(Encoding.UTF8.GetString(data.ToArray()));

        public byte[] Serialize(T data, SerializationContext context) =>
            Encoding.UTF8.GetBytes(Core.Configuration.JsonAdapter.Serialize(data));
    }
}
