using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Audit.Core
{
    public class JsonNewtonsoftAdapter : IJsonAdapter
    {
        public JsonSerializerSettings Settings { get; set; }

        public JsonNewtonsoftAdapter()
        {
            Settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }
        public JsonNewtonsoftAdapter(JsonSerializerSettings settings)
        {
            Settings = settings;
        }
        
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, Settings);
        }

        public async Task SerializeAsync(Stream stream, object value)
        {
            var json = JsonConvert.SerializeObject(value, Settings);
            using (StreamWriter sw = new StreamWriter(stream))
            {
                await sw.WriteAsync(json);
            }
        }

        public async Task<T> DeserializeAsync<T>(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    var jObject = await JObject.LoadAsync(jr);
                    return jObject.ToObject<T>(JsonSerializer.Create(Settings));
                }
            }
        }

        public T ToObject<T>(object value)
        {
            if (value == null)
            {
                return default;
            }
            if (value is T || typeof(T).IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }
			if (value is JContainer)
			{
				return (value as JContainer).ToObject<T>();
			}
            return default(T);
        }
    }
}
