using System;
using Audit.WebApi.Template.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if (EnableEntityFramework)
using Microsoft.EntityFrameworkCore;
using Audit.WebApi.Template.Providers.Database;
#endif

namespace Audit.WebApi.Template
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IValuesProvider, ValuesProvider>();

#if (EnableEntityFramework)
            // TODO: Configure your context connection
            services.AddDbContext<MyContext>(_ => _.UseInMemoryDatabase("default"));
#endif
            services.AddMvcAudit();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IHttpContextAccessor contextAccessor)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.AddAuditCorrelationId(contextAccessor);
        }
    }
}
