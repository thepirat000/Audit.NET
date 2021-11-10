using Audit.Core.ConfigurationApi;
using System.Text.Json;

namespace Audit.Core
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Use System.Text.Json as the default JSON serializer.
        /// </summary>
        /// <param name="config">The IConfigurator instance.</param>
        /// <param name="options">The JSON serialization options.</param>
        public static IConfigurator JsonSystemAdapter(this IConfigurator config, JsonSerializerOptions options)
        {
            Configuration.JsonAdapter = new JsonSystemAdapter(options);
            return config;
        }
        /// <summary>
        /// Use System.Text.Json as the default JSON serializer.
        /// </summary>
        public static IConfigurator JsonSystemAdapter(this IConfigurator config)
        {
            Configuration.JsonAdapter = new JsonSystemAdapter();
            return config;
        }
    }
}
