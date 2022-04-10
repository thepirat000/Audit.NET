using Audit.Core.ConfigurationApi;
using Newtonsoft.Json;

namespace Audit.Core
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Use Newtonsoft.Json as the default JSON serializer.
        /// If the given JsonSerializerSettings doesn't have a ContractResolver assigned, it will be assigned an instance of <see cref="Audit.JsonNewtonsoftAdapter.AuditContractResolver"/>
        /// for compatibility with <see cref="JsonExtensionDataAttribute"/> and <see cref="JsonIgnoreAttribute"/> from System.Text.Json
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
        /// This adapter will register a custom IContractResolver (<see cref="Audit.JsonNewtonsoftAdapter.AuditContractResolver"/>)
        /// that honors <see cref="JsonExtensionDataAttribute"/> and <see cref="JsonIgnoreAttribute"/> from System.Text.Json
        /// </summary>
        public static IConfigurator JsonNewtonsoftAdapter(this IConfigurator config)
        {
            Configuration.JsonAdapter = new JsonNewtonsoftAdapter();
            return config;
        }
    }
}
