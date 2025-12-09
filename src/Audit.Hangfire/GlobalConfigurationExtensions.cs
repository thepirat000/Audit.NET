using Audit.Hangfire.ConfigurationApi;

using Hangfire;

using System;

namespace Audit.Hangfire
{
    public static class GlobalConfigurationExtensions
    {
        /// <summary>
        /// Adds the AuditJobCreationFilter to the Hangfire global configuration.
        /// </summary>
        /// <param name="globalConfiguration">Hangfire global configuration.</param>
        /// <param name="configurator">Configurator action to set up the audit job creation options.</param>
        public static IGlobalConfiguration AddAuditJobCreationFilter(this IGlobalConfiguration globalConfiguration, Action<IAuditHangfireJobCreationConfigurator> configurator)
        {
            return globalConfiguration.UseFilter(new AuditJobCreationFilterAttribute(configurator));
        }

        /// <summary>
        /// Adds the AuditJobExecutionFilter to the Hangfire global configuration.
        /// </summary>
        /// <param name="globalConfiguration">Hangfire global configuration.</param>
        /// <param name="configurator">Configurator action to set up the audit job execution options.</param>
        public static IGlobalConfiguration AddAuditJobExecutionFilter(this IGlobalConfiguration globalConfiguration, Action<IAuditHangfireJobExecutionConfigurator> configurator)
        {
            return globalConfiguration.UseFilter(new AuditJobExecutionFilterAttribute(configurator));
        }

    }
}
