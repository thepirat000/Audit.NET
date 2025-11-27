#if NET6_0_OR_GREATER
using Audit.Grpc.Server.UnitTest;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

// Add gRPC services and register your interceptor
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<TestDemoService>();

app.Run();

public partial class Program { }
#else

#endif

