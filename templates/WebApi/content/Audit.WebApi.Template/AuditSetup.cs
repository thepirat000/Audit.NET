using Audit.Core.Providers;
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
        /// Adds an audited service to the service collection. The service will be intercepted to log the calls to its methods.
        /// </summary>
        public static IServiceCollection AddScopedAuditedService<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddScoped<TService>(s =>
            {
                // AuditProxy lacks awareness of the service provider, defaulting to the globally configured ScopeFactory and DataProvider.
                // To prevent this, we retrieve the ScopeFactory and DataProvider from the service provider and set them in the InterceptionSettings.
                var interceptionSettings = new InterceptionSettings()
                {
                    EventType = EventTypeServiceInterception,
                    AuditScopeFactory = s.GetRequiredService<IAuditScopeFactory>(),
                    AuditDataProvider = s.GetRequiredService<AuditDataProvider>()
                };

                var service = (TService)ActivatorUtilities.CreateInstance<TImplementation>(s);

                return AuditProxy.Create(service, interceptionSettings);
            });
        }
#endif

        /// <summary>
        /// Add the global audit filter to the MVC pipeline
        /// </summary>
        public static MvcOptions AuditSetupMvcFilter(this MvcOptions mvcOptions)
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
        /// Setups the audit scope creation
        /// </summary>
        public static IServiceCollection AddAuditScopeFactory(this IServiceCollection services)
        {
            services.AddScoped<IAuditScopeFactory, MyAuditScopeFactory>();

            return services;
        }

        /// <summary>
        /// Setups the audit output
        /// </summary>
        public static IServiceCollection AddAuditDataProvider(this IServiceCollection services)
        {
            Audit.Core.Configuration.JsonSettings.WriteIndented = true;

            services.AddSingleton<IAuditDataProvider>(new FileDataProvider(cfg => cfg
                .Directory(@"C:\Logs")
                .FilenameBuilder(ev => $"{ev.StartDate:yyyyMMddHHmmssffff}_{ev.EventType}.json")));

            return services;
        }
    }
}
