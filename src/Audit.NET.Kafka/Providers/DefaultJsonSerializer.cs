using Confluent.Kafka;
using System.Text;
using Newtonsoft.Json;
using System;

namespace Audit.Kafka.Providers
{
    public class DefaultJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
            isNull ? default : JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data.ToArray()), Audit.Core.Configuration.JsonSettings);

        public byte[] Serialize(T data, SerializationContext context) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, Audit.Core.Configuration.JsonSettings));
    }
}
