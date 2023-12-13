#if NETCOREAPP3_1
using System;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Wcf.UnitTest
{
    public class Startup
    {
        public const int HTTP_PORT = 8733;
        public const int NETTCP_PORT = 8089;
        public const string HOST_IN_WSDL = "localhost";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices()
                .AddServiceModelMetadata()
                .AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseServiceModel(builder =>
            {
                builder.AddService<CatalogService>(serviceOptions =>
                    {
                        serviceOptions.BaseAddresses.Add(new Uri($"http://{HOST_IN_WSDL}/CatalogService"));
                    })
                    .AddServiceEndpoint<CatalogService, ICatalogService>(new CoreWCF.BasicHttpBinding(), "/basichttp");
                var serviceMetadataBehavior = app.ApplicationServices.GetRequiredService<ServiceMetadataBehavior>();
                serviceMetadataBehavior.HttpGetEnabled = serviceMetadataBehavior.HttpsGetEnabled = true;
            });
        }
    }
}
#endif