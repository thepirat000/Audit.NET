using System;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Text.Json;

namespace Audit.Core
{
    public class JsonAdapter : IJsonAdapter
    {
        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, value.GetType(), Configuration.JsonSettings);
        }
        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Configuration.JsonSettings);
        }
        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, Configuration.JsonSettings);
        }

        public async Task SerializeAsync(Stream stream, object value, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), Configuration.JsonSettings, cancellationToken);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, Configuration.JsonSettings, cancellationToken);
        }

        public T ToObject<T>(object value)
        {
			if (value == null)
			{
				return default;
			}
			if (value is T || typeof(T).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
			{
				return (T)value;
			}
            if (value is JsonElement element)
            {
                return element.Deserialize<T>(Configuration.JsonSettings);
            }

            return default;
		}
    }
}
