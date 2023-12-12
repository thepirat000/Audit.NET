using System;
using System.Collections.Generic;
using System.Text;
using Audit.Core;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Threading;

#if IS_NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Buffers;
#endif

namespace Audit.Core
{
    public class JsonAdapter : IJsonAdapter
    {
#if IS_NK_JSON
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Configuration.JsonSettings);
        }
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Configuration.JsonSettings);
        }
        public object Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, Configuration.JsonSettings);
        }
#else
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
#endif

        public async Task SerializeAsync(Stream stream, object value, CancellationToken cancellationToken = default)
        {
#if IS_NK_JSON
            var json = JsonConvert.SerializeObject(value, Configuration.JsonSettings);
            using (StreamWriter sw = new StreamWriter(stream))
            {
                await sw.WriteAsync(json);
            }
#else
            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), Configuration.JsonSettings, cancellationToken);
#endif
        }

        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
#if IS_NK_JSON
            using (var sr = new StreamReader(stream))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    var jObject = await JObject.LoadAsync(jr, cancellationToken);
                    return jObject.ToObject<T>(JsonSerializer.Create(Configuration.JsonSettings));
                }
            }
#else
            return await JsonSerializer.DeserializeAsync<T>(stream, Configuration.JsonSettings, cancellationToken);
#endif
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
#if IS_NK_JSON 
			if (value is JContainer container)
			{
				return container.ToObject<T>(JsonSerializer.Create(Configuration.JsonSettings));
			}
#elif NET6_0_OR_GREATER
            if (value is JsonElement element)
            {
                return element.Deserialize<T>(Configuration.JsonSettings);
            }
#elif NETSTANDARD2_0
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText(), Configuration.JsonSettings);
            }
#else
            // Workaround for to convert from JsonElement to Object (https://github.com/dotnet/runtime/issues/31274)
            if (value is JsonElement element)
            {
                
                var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
			    using (var writer = new Utf8JsonWriter(bufferWriter))
			    {
    				element.WriteTo(writer);
			    }
			    return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, Configuration.JsonSettings);
            }
#endif
            return default;
		}

    }
}
