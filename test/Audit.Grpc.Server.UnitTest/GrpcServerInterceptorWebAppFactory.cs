#if NET6_0_OR_GREATER
using Audit.Core.Providers;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Audit.Grpc.Server.UnitTest;

/// <summary>
/// Web application factory for gRPC testing.
/// </summary>
public class GrpcServerInterceptorWebAppFactory : WebApplicationFactory<Program>
{
    public InMemoryDataProvider DataProvider { get; } = new InMemoryDataProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Register interceptor 
            services.AddSingleton(_ =>
                new AuditServerInterceptor(cfg => cfg
                    .CallFilter(_ => true)
                    .AuditDataProvider(DataProvider)
                    .IncludeRequestHeaders()
                    .IncludeRequestPayload()
                    .IncludeResponsePayload()
                    .IncludeTrailers()
                ));

            // Ensure gRPC services use the interceptor
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<AuditServerInterceptor>();
            });
        });
    }

    public GrpcChannel CreateGrpcChannel()
    {
        var client = this.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        var httpHandler = this.Server.CreateHandler();
        var channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = httpHandler,
            Credentials = ChannelCredentials.Insecure
        });
        return channel;
    }
}
#endif