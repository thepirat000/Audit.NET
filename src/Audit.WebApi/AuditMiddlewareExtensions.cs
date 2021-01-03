#if ASP_CORE
using Microsoft.AspNetCore.Builder;
using System;

namespace Audit.WebApi
{
    public static class AuditMiddlewareExtensions
    {
        /// <summary>
        /// Add the Audit middleware to the pipeline. 
        /// This can be used together with AuditApi action filter.
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <param name="config">The audit middleware configuration</param>
        public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder builder, Action<ConfigurationApi.IAuditMiddlewareConfigurator> config)
        {
            var mwConfig = new ConfigurationApi.AuditMiddlewareConfigurator();
            config.Invoke(mwConfig);
            return builder.UseMiddleware<AuditMiddleware>(mwConfig);
        }

    }
}
#endif