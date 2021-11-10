using System;
using System.Collections.Generic;
using System.Text;
using Audit.Core;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
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

        public async Task SerializeAsync(Stream stream, object value)
        {
#if IS_NK_JSON
            var json = JsonConvert.SerializeObject(value, Configuration.JsonSettings);
            using (StreamWriter sw = new StreamWriter(stream))
            {
                await sw.WriteAsync(json);
            }
#else
            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), Configuration.JsonSettings);
#endif
        }

        public async Task<T> DeserializeAsync<T>(Stream stream)
        {
#if IS_NK_JSON
            using (var sr = new StreamReader(stream))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    var jObject = await JObject.LoadAsync(jr);
                    return jObject.ToObject<T>(JsonSerializer.Create(Configuration.JsonSettings));
                }
            }
#else
            return await JsonSerializer.DeserializeAsync<T>(stream, Configuration.JsonSettings);
#endif
        }

        public T ToObject<T>(object value)
        {
			if (value == null)
			{
				return default(T);
			}
			if (value is T || typeof(T).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
			{
				return (T)value;
			}
#if IS_NK_JSON
			if (value is JContainer)
			{
				return (value as JContainer).ToObject<T>(JsonSerializer.Create(Configuration.JsonSettings));
			}
#else
			// TODO: Workaround to convert from JsonElement to Object, until https://github.com/dotnet/runtime/issues/31274 fixed
			if (value is JsonElement)
			{
				var element = (JsonElement)value;
				var bufferWriter = new ArrayBufferWriter<byte>();
				using (var writer = new Utf8JsonWriter(bufferWriter))
				{
					element.WriteTo(writer);
				}
				return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, Configuration.JsonSettings);
			}
#endif
			return default(T);
		}

    }
}
