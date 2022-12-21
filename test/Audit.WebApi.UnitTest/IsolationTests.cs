#if NET5_0_OR_GREATER
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.WebApi.UnitTest
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TestIsolationController : ControllerBase
    {
        [AuditApi]
        [HttpGet]
        public IActionResult Action_AuditApiAttribute([FromQuery] string q)
        {
            return Ok(q);
        }

        public IActionResult Action_Middleware([FromQuery] string q)
        {
            return Ok(q);
        }
    }

    public class TestHelper
    {
        public static TestServer GetTestServer(AuditDataProvider dataProvider)
        {
            return new TestServer(WebHost.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(dataProvider);
                    services.AddControllers();
                })
                .Configure((ctx, app) =>
                {
                    app.UseAuditMiddleware(cfg => cfg
                        .FilterByRequest(r => r.Path.Value?.Contains("Action_Middleware") == true));
                    app.UseRouting();
                    app.UseEndpoints(e =>
                    {
                        e.MapControllers();
                    });
                })
            );
        }
    }

    [Parallelizable]
    public class IsolationTests
    {
        [TestCase(10)]
        public async Task Test_Isolation_InjectDataProvider_AuditApiAttribute_Parallel(int count)
        {
            var tasks = Enumerable.Range(1, count).Select(_ => Test_Isolation_InjectDataProvider_AuditApiAttribute_TestOne()).ToArray();
            await Task.WhenAll(tasks);
        }

        private async Task Test_Isolation_InjectDataProvider_AuditApiAttribute_TestOne()
        {
            var guid = Guid.NewGuid();
            var dataProvider = new InMemoryDataProvider();
            using var app = TestHelper.GetTestServer(dataProvider);
            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestIsolation/Action_AuditApiAttribute?q={guid}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, dataProvider.GetAllEvents().Count);
            Assert.AreEqual(guid.ToString(), dataProvider.GetAllEventsOfType<AuditEventWebApi>().First().Action.ActionParameters["q"].ToString());
        }

        [TestCase(10)]
        public async Task Test_Isolation_InjectDataProvider_Middleware_Parallel(int count)
        {
            var tasks = Enumerable.Range(1, count).Select(_ => Test_Isolation_InjectDataProvider_Middleware_TestOne()).ToArray();
            await Task.WhenAll(tasks);
        }

        private async Task Test_Isolation_InjectDataProvider_Middleware_TestOne()
        {
            var guid = Guid.NewGuid();
            var dataProvider = new InMemoryDataProvider();
            using var app = TestHelper.GetTestServer(dataProvider);
            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestIsolation/Action_Middleware?q={guid}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, dataProvider.GetAllEvents().Count);
            Assert.IsTrue(dataProvider.GetAllEventsOfType<AuditEventWebApi>().First().Action.RequestUrl.Contains(guid.ToString()));
        }
    }
}
#endif