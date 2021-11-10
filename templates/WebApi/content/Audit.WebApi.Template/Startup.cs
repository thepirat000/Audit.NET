using System;
using Audit.WebApi.Template.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (EnableEntityFramework)
using Microsoft.EntityFrameworkCore;
using Audit.WebApi.Template.Providers.Database;
#endif
#if (Swagger)
using System.IO;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
#endif

namespace Audit.WebApi.Template
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<IValuesProvider, ValuesProvider>();

#if (EnableEntityFramework)
            // TODO: Configure your context connection
            services.AddDbContext<MyContext>(_ => _.UseInMemoryDatabase("default"));
#endif
#if (Swagger)
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Audit.WebApi.Template API",
                    Description = "Audit.WebApi.Template API"
                });
                var basePath = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(basePath, "Audit.WebApi.Template.xml");
                c.IncludeXmlComments(xmlPath);
                c.DescribeAllParametersInCamelCase();
            });
#endif
            services
                .ConfigureAudit()
                .AddMvc(options => 
                { 
                    options.AddAudit();
                    options.EnableEndpointRouting = false;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHttpContextAccessor contextAccessor)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if (Swagger)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit.WebApi.Template API");
            });
#endif
            app.Use(async (context, next) => {
                context.Request.EnableBuffering();
                await next();
            });

            app.UseAuditMiddleware();

            app.UseMvc();
            app.UseAuditCorrelationId(contextAccessor);
        }
    }
}
