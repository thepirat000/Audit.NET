using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Integration.AspNetCore.Controllers;
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Audit.Integration.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = 2;
            });

            services.AddMvc(mvc =>
            {
                mvc.Filters.Add(new AuditIgnoreActionFilter_ForTest());
                mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                    .LogActionIf(d => d.ControllerName == "MoreValues"
                        || (d.ControllerName == "Mvc" && d.ActionName == "Details")
                        || (d.ControllerName == "Values" &&
                                (d.ActionName == "GlobalAudit" || d.ActionName == "TestForm" || d.ActionName.StartsWith("TestIgnore") || d.ActionName.StartsWith("PostMix") || d.ActionName == "TestResponseHeadersGlobalFilter")))
                    .WithEventType("{verb}.{controller}.{action}")
                    .IncludeHeaders()
                    .IncludeResponseHeaders()
                    .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode == 200)
                    .IncludeRequestBody()));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.Use(async (context, next) => {
                context.Request.EnableRewind();
                await next();
            });
            app.UseAuditMiddleware(_ => _
                .IncludeRequestBody(true)
                .IncludeResponseBody(ctx => !ctx.Request.QueryString.HasValue || !ctx.Request.QueryString.Value.ToLower().Contains("noresponsebody"))
                .IncludeHeaders(true)
                .IncludeResponseHeaders()
                .WithEventType("{verb}.{url}")
                .FilterByRequest(r => r.QueryString.HasValue && r.QueryString.Value.ToLower().Contains("middleware")));

            app.UseMvc();
        }
    }
}
