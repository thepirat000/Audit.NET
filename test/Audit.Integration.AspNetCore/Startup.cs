using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Integration.AspNetCore.Controllers;
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddMvc(mvc =>
            {
                mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                //mvc.AddAuditFilter(config => config
                    .LogActionIf(d => d.ControllerName == "Values" && d.ActionName == "GlobalAudit")
                    .WithEventType("{verb}.{controller}.{action}")
                    .IncludeHeaders()
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
       
            app.UseMvc();
        }
    }
}
