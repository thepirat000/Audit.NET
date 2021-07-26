using Audit.Core.ConfigurationApi;
using Newtonsoft.Json;

namespace Audit.Core
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Use Newtonsoft.Json as the default JSON serializer.
        /// </summary>
        /// <param name="settings">The JSON serialization settings.</param>
        /// <param name="config">The IConfigurator instance.</param>
        public static IConfigurator JsonNewtonsoftAdapter(this IConfigurator config, JsonSerializerSettings settings)
        {
            Configuration.JsonAdapter = new JsonNewtonsoftAdapter(settings);
            return config;
        }
        /// <summary>
        /// Use Newtonsoft.Json as the default JSON serializer.
        /// </summary>
        public static IConfigurator JsonNewtonsoftAdapter(this IConfigurator config)
        {
            Configuration.JsonAdapter = new JsonNewtonsoftAdapter();
            return config;
        }
    }
}
