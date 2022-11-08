using Microsoft.AspNetCore.Mvc;

namespace Audit.WebApi.Template
{
    /// <summary>
    /// Audit.NET setup
    /// </summary>
    public static class AuditSetup
    {
        /// <summary>Event type to identify the MVC audit logs</summary>
        private const string EventTypeMvc = "MVC";
        /// <summary>Event type to identify the HTTP audit logs from the middleware</summary>
        private const string EventTypeHttp = "HTTP";
#if EnableEntityFramework
        /// <summary>Event type to identify the Entity Framework audit logs</summary>
        private const string EventTypeEntityFramework = "EF";
#endif
#if ServiceInterception
        /// <summary>Event type to identify the Service interception audit logs</summary>
        private const string EventTypeServiceInterception = "SVC";
#endif

#if ServiceInterception
        /// <summary>
        /// Adds an audited service to the service collection
        /// </summary>
        public static IServiceCollection AddAuditedTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransient<TService>(s =>
            {
                var svc = (TService)ActivatorUtilities.CreateInstance<TImplementation>(s);
                return AuditProxy.Create(svc, new InterceptionSettings()
                {
                    EventType = EventTypeServiceInterception
                });
            });
        }
#endif

        /// <summary>
        /// Add the global audit filter to the MVC pipeline
        /// </summary>
        public static MvcOptions AuditSetupFilter(this MvcOptions mvcOptions)
        {
            // Add the global MVC Action Filter to the filter chain
            mvcOptions.AddAuditFilter(a => a
                .LogAllActions()
                .WithEventType(EventTypeMvc)
                .IncludeModelState()
                .IncludeRequestBody()
                .IncludeResponseBody());

            return mvcOptions;
        }

        /// <summary>
        /// Add the audit middleware to the pipeline
        /// </summary>
        public static void AuditSetupMiddleware(this IApplicationBuilder app)
        {
            // Add the audit Middleware to the pipeline
            app.UseAuditMiddleware(_ => _
                .FilterByRequest(r => !r.Path.Value!.EndsWith("favicon.ico"))
                .WithEventType(EventTypeHttp)
                .IncludeHeaders()
                .IncludeRequestBody()
                .IncludeResponseBody());
        }

#if EnableEntityFramework
        /// <summary>
        /// Setup the Entity Framework DbContext audit
        /// </summary>
        public static void AuditSetupDbContext(this IApplicationBuilder app)
        {
            // Configure the Entity framework audit.
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyContext>(_ => _
                    .AuditEventType(EventTypeEntityFramework)
                    .IncludeEntityObjects())
                .UseOptOut();
        }
#endif

        /// <summary>
        /// Setups the audit output
        /// </summary>
        public static void AuditSetupOutput(this WebApplication app)
        {
            // TODO: Configure the audit output.
            // For more info, see https://github.com/thepirat000/Audit.NET#data-providers.
            Audit.Core.Configuration.Setup()
                .UseFileLogProvider(_ => _
                    .Directory(@"C:\Logs")
                    .FilenameBuilder(ev => $"{ev.StartDate:yyyyMMddHHmmssffff}_{ev.EventType}.json"));

            Audit.Core.Configuration.JsonSettings.WriteIndented = true;

            // Include the trace identifier in the audit events
            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                scope.SetCustomField("TraceId", httpContextAccessor.HttpContext?.TraceIdentifier);
            });
        }
    }
}
