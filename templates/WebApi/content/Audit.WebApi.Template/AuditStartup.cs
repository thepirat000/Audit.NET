using Audit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Audit.WebApi;
#if (EnableEntityFramework)
using Audit.WebApi.Template.Providers.Database;
#endif

namespace Audit.WebApi.Template
{
    public static class AuditStartup
    {
        private const string CorrelationIdField = "CorrelationId";

        /// <summary>
        /// Add the global audit filter to the MVC pipeline
        /// </summary>
        public static MvcOptions AddAudit(this MvcOptions mvcOptions)
        {
            mvcOptions.AddAuditFilter(a => a
                    .LogAllActions()
                    .WithEventType("MVC:{verb}:{controller}:{action}")
                    .IncludeHeaders()
                    .IncludeModelState()
                    .IncludeRequestBody()
                    .IncludeResponseBody());
            return mvcOptions;
        }

        /// <summary>
        /// Global Audit configuration
        /// </summary>
        public static IServiceCollection ConfigureAudit(this IServiceCollection serviceCollection)
        {
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

            return serviceCollection;
        }

        /// <summary>
        /// Add a RequestId so the audit events can be grouped per request
        /// </summary>
        public static void UseAuditCorrelationId(this IApplicationBuilder app, IHttpContextAccessor ctxAccesor)
        {
            
            Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                var httpContext = ctxAccesor.HttpContext;
                scope.Event.CustomFields[CorrelationIdField] = httpContext.TraceIdentifier;
            });
        }
    }
}
