using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace Audit.Core
{
    public class JsonSystemAdapter : IJsonAdapter
    {
        public JsonSerializerOptions Settings { get; set; }

        public JsonSystemAdapter()
        {
            Settings = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null,
                // TODO: Will be added on .net 6 https://github.com/dotnet/runtime/pull/46101/commits/152db423e06f6d93a560b45b4330fac6507c7aa7
                //ReferenceHandler = ReferenceHandler.IgnoreCycle
            };
        }
        public JsonSystemAdapter(JsonSerializerOptions settings)
        {
            Settings = settings;
        }

        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, value.GetType(), Settings);
        }
        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Settings);
        }
        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, Settings);
        }

        public async Task SerializeAsync(Stream stream, object value, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), Settings, cancellationToken);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, Settings, cancellationToken);
        }

        public T ToObject<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }
            if (value is T || typeof(T).IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }
            // TODO: Workaround to convert from JsonElement to Object, until https://github.com/dotnet/runtime/issues/31274 fixed
            if (value is JsonElement)
            {
                var element = (JsonElement)value;
                using (var bufferWriter = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(bufferWriter))
                    {
                        element.WriteTo(writer);
                    }
                    return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bufferWriter.ToArray()), Settings);
                }
            }
            return default(T);
        }
    }
}
