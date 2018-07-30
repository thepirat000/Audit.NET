using Audit.Core;
using Audit.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
#if (EnableEntityFramework)
using Audit.WebApi.Template.Providers.Database;
#endif

namespace Audit.WebApi.Template
{
    public static class AuditStartup
    {
        private const string CorrelationIdField = "CorrelationId";

        public static void AddMvcAudit(this IServiceCollection serviceCollection)
        {
            // Audit general configuration
            // TODO: Configure the audit data provider and options. For more info see https://github.com/thepirat000/Audit.NET#data-providers.
            Audit.Core.Configuration.Setup()
                .UseFileLogProvider(_ => _
                    .Directory(@"C:\Temp")
                    .FilenameBuilder(ev => $"{ev.StartDate:yyyyMMddHHmmssffff}_{ev.CustomFields[CorrelationIdField]?.ToString().Replace(':', '_')}.json"))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

#if (EnableEntityFramework)
            // Entity framework audit output configuration
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyContext>(_ => _
                    .AuditEventType("EF:{context}"))
                .UseOptOut();
#endif

            // Add Mvc services. Audit enabled for all the actions.
            serviceCollection
                .AddMvc(_ => _
                    .AddAuditFilter(a => a
                        .LogAllActions()
                        .WithEventType("MVC:{verb}:{controller}:{action}")
                        .IncludeHeaders()
                        .IncludeModelState()
                        .IncludeRequestBody()
                        .IncludeResponseBody()))
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add a RequestId so the audit events can be grouped per request
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                var httpContext = serviceProvider.GetService<IHttpContextAccessor>().HttpContext;
                scope.Event.CustomFields[CorrelationIdField] = httpContext.TraceIdentifier;
            });
        }

    }
}
