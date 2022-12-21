using System.Linq;
using Audit.Core;
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Audit.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Microsoft.CodeAnalysis;

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
                mvc.InputFormatters.Insert(0, GetJsonPatchInputFormatter());

                mvc.EnableEndpointRouting = false;
                mvc.Filters.Add(new AuditIgnoreActionFilter_ForTest());
                mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                    .LogActionIf(d => d.ControllerName == "MoreValues"
                        || (d.ControllerName == "Mvc" && d.ActionName == "Details")
                        || (d.ControllerName == "Values" &&
                                (d.ActionName == "GlobalAudit" || d.ActionName == "TestForm" || d.ActionName.StartsWith("TestIgnore") || d.ActionName.StartsWith("PostMix") || d.ActionName == "TestResponseHeadersGlobalFilter"
                                || d.ActionName == "TestDoNotSerializeParams")))
                    .WithEventType("{verb}.{controller}.{action}")
                    .IncludeHeaders()
                    .IncludeResponseHeaders()
                    .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode == 200)
                    .IncludeRequestBody()));
                
                mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                    .LogActionIf(d => d.ControllerName == "Values" && d.ActionName == "TestSerializeParams")
                    .SerializeActionParameters(true)
                ));
            }).AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            services.AddRazorPages(options =>
            {
                options.Conventions.AddFolderApplicationModelConvention("/PageTest", model => model.Filters.Add(new AuditPageFilter()
                {
                    IncludeHeaders = true,
                    IncludeModel = true,
                    IncludeRequestBody = true,
                    IncludeResponseBody = true,
                    EventTypeName = "{verb}:{path}"
                }));
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.Use(async (context, next) => {
                context.Request.EnableBuffering();
                await next();
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            app.UseWhen(ctx => ctx.Request.Headers.ContainsKey("UseErrorHandler"), a => a.UseMiddleware<ApiErrorHandlerMiddleware_Test>());

            app.UseAuditMiddleware(_ => _
                .IncludeRequestBody(true)
                .IncludeResponseBody(ctx => !ctx.Request.QueryString.HasValue || !ctx.Request.QueryString.Value.ToLower().Contains("noresponsebody"))
                .IncludeHeaders(true)
                .IncludeResponseHeaders()
                .WithEventType("{verb}.{url}")
                .FilterByRequest(r => r.QueryString.HasValue && r.QueryString.Value.ToLower().Contains("middleware")));

            app.UseMvc();

        }

        private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
        {
            var builder = new ServiceCollection()
                .AddLogging()
                .AddMvc()
                .AddNewtonsoftJson()
                .Services.BuildServiceProvider();

            return builder
                .GetRequiredService<IOptions<MvcOptions>>()
                .Value
                .InputFormatters
                .OfType<NewtonsoftJsonPatchInputFormatter>()
                .First();
        }
    }
}
