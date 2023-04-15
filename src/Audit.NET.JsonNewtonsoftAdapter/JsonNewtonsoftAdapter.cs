using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Audit.JsonNewtonsoftAdapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Audit.Core
{
    /// <summary>
    /// Adapter to serialize Audit Events using Newtonsoft.Json instead of the default System.Text.Json.
    /// This adapter will register and use a custom IContractResolver that honors <see cref="JsonExtensionDataAttribute"/> and <see cref="JsonIgnoreAttribute"/> from <see cref="System.Text.Json.Serialization"/>
    /// </summary>
    public class JsonNewtonsoftAdapter : IJsonAdapter
    {
        public JsonSerializerSettings Settings { get; set; }

        public JsonNewtonsoftAdapter()
        {
            Settings = new JsonSerializerSettings()
            {
                ContractResolver = new AuditContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public JsonNewtonsoftAdapter(JsonSerializerSettings settings)
        {
            if (settings.ContractResolver == null)
            {
                settings.ContractResolver = new AuditContractResolver();
            }
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

        public async Task SerializeAsync(Stream stream, object value, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(value, Settings);
            using (StreamWriter sw = new StreamWriter(stream))
            {
                await sw.WriteAsync(json);
            }
        }

        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            using (var sr = new StreamReader(stream))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    var jObject = await JObject.LoadAsync(jr, cancellationToken);
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
