#if NET6_0_OR_GREATER
using System;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Audit.Grpc.Client.UnitTest;

/// <summary>
/// Web application factory for gRPC testing.
/// </summary>
public class GrpcWebAppFactoryFixture : WebApplicationFactory<Program>, IDisposable
{
    public GrpcChannel CreateGrpcChannel()
    {
        var options = new WebApplicationFactoryClientOptions()
        {
            BaseAddress = new Uri("http://localhost")
        };
        var client = this.CreateClient(options);
        // Configure HTTP handler for HTTP/2
        var httpHandler = this.Server.CreateHandler();
        var channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = httpHandler,
            Credentials = ChannelCredentials.Insecure
        });
        return channel;
    }

    public new void Dispose()
    {
        base.Dispose();
    }
}
#endif