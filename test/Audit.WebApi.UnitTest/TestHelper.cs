#if NETCOREAPP3_1 || NET6_0
using System;
using Audit.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Audit.WebApi.UnitTest
{
    public class TestHelper
    {
        public static TestServer GetTestServer(IAuditDataProvider dataProvider, Action<ConfigurationApi.IAuditMiddlewareConfigurator> middlewareConfig,
            IAuditScopeFactory scopeFactory = null)
        {
            return new TestServer(WebHost.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(dataProvider);
                    if (scopeFactory != null)
                    {
                        services.AddSingleton(scopeFactory);
                    }
                    services.AddControllers();
                })
                .Configure((ctx, app) =>
                {
                    app.UseAuditMiddleware(middlewareConfig);
                    app.UseRouting();
                    app.UseEndpoints(e =>
                    {
                        e.MapControllers();
                    });
                })
                .ConfigureLogging(log => log.SetMinimumLevel(LogLevel.Warning))
            );
        }
    }
}
#endif