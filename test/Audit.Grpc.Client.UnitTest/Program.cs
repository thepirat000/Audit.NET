#if NET6_0_OR_GREATER
using System;
using Audit.Grpc.Client;
using Audit.Grpc.Client.ConfigurationApi;
using Audit.Grpc.Client.UnitTest;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TestGrpcService.Protos;

var builder = WebApplication.CreateBuilder();

Action<IAuditClientInterceptorConfigurator> config = c => c.IncludeRequestHeaders();

//builder.Services.AddGrpcClient<DemoService.DemoServiceClient>().AddInterceptor(svc => new AuditClientInterceptor());

// Add gRPC services and register your interceptor
builder.Services.AddGrpc(options =>
{
    //options.Interceptors.Add<AuditClientInterceptor>(config);
});

var app = builder.Build();

app.MapGrpcService<TestDemoService>();

app.Run();
public partial class Program { }
#else

#endif

