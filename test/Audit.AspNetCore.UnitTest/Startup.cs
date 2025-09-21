using Audit.Mvc;
using Audit.WebApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System.Linq;

namespace Audit.AspNetCore.UnitTest
{
    public class Startup
    {
        private bool _isMvc;
        private bool _isWebApi;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _isMvc = configuration.GetValue<bool>("IsMvc");
            _isWebApi = configuration.GetValue<bool>("IsWebApi");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = 2;
            });

            if (_isMvc || _isWebApi)
            {
                services.AddMvc(mvc =>
                {
                    mvc.InputFormatters.Insert(0, GetJsonPatchInputFormatter());

                    mvc.EnableEndpointRouting = false;
                    mvc.Filters.Add(new AuditIgnoreActionFilter_ForTest());
                    mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                        .LogActionIf(d => d.ControllerName == "MoreValues"
                                          || (d.ControllerName == "Mvc" && d.ActionName == "Details")
                                          || (d.ControllerName == "Values" &&
                                              (d.ActionName == "GlobalAudit" || d.ActionName == "TestForm" ||
                                               d.ActionName.StartsWith("TestIgnore") ||
                                               d.ActionName.StartsWith("PostMix") ||
                                               d.ActionName == "TestResponseHeadersGlobalFilter"
                                               || d.ActionName == "TestDoNotSerializeParams")))
                        .WithEventType("{verb}.{controller}.{action}")
                        .IncludeHeaders()
                        .IncludeResponseHeaders()
                        .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode == 200)
                        .IncludeRequestBody()
                        .IncludeModelState(false)));

                    mvc.Filters.Add(new AuditApiGlobalFilter(config => config
                        .LogActionIf(d => d.ControllerName == "Values" && d.ActionName == "TestSerializeParams")
                        .SerializeActionParameters(true)
                    ));
                }).AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.DefaultIgnoreCondition =
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    o.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
            }

            if (_isMvc)
            {
                services.AddRazorPages(options =>
                {
                    options.Conventions.AddFolderApplicationModelConvention("/PageTest", model =>
                        model.Filters.Add(new AuditPageFilter()
                        {
                            IncludeHeaders = true,
                            IncludeModel = true,
                            IncludeRequestBody = true,
                            IncludeResponseBody = true,
                            EventTypeName = "{verb}:{path}"
                        }));
                });
            }
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

            if (_isMvc)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                });
            }
            
            app.UseWhen(ctx => ctx.Request.Headers.ContainsKey("UseErrorHandler"), a => a.UseMiddleware<ApiErrorHandlerMiddleware_Test>());

            if (_isMvc || _isWebApi)
            {
                app.UseAuditMiddleware(_ => _
                    .IncludeRequestBody(true)
                    .IncludeResponseBody(ctx =>
                        !ctx.Request.QueryString.HasValue ||
                        !ctx.Request.QueryString.Value.ToLower().Contains("noresponsebody"))
                    .IncludeHeaders(_ => true)
                    .IncludeResponseHeaders(_ => true)
                    .WithEventType("{verb}.{url}")
                    .FilterByRequest(
                        r => r.QueryString.HasValue && r.QueryString.Value.ToLower().Contains("middleware")));
            }

            if (_isMvc || _isWebApi)
            {
                app.UseMvc();
            }
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
