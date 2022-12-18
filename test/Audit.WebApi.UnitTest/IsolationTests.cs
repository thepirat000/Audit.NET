#if NET5_0
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

// this test shall run execution in parallel and check if isolated data providers are correctly implemented

namespace Audit.WebApi.UnitTest
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class Test2Controller : ControllerBase
    {
        [AuditApi]
        [HttpGet]
        public IActionResult Action()
        {
            return Ok();
        }
    }
    
    public class TestHelper
    {
        public static TestServer GetTestServer()
        {
            return new TestServer(WebHost.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                })
                .Configure((ctx, app) =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e =>
                    {
                        e.MapControllers();
                    });
                })
            );
        }
    }
    
    [Parallelizable(ParallelScope.Children)]
    public class IsolationTests
    {
        [Test]
        public async Task Test1()
        {
            Configuration.Setup().UseInMemoryProvider();
            
            using var app = TestHelper.GetTestServer();
            using var client = app.CreateClient();

            var response = await client.GetAsync("/test2/action");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, ((InMemoryDataProvider)Configuration.DataProvider).GetAllEvents().Count);
        }
        
        [Test]
        public async Task Test2()
        {
            Configuration.Setup().UseInMemoryProvider();
            
            using var app = TestHelper.GetTestServer();
            using var client = app.CreateClient();

            var response = await client.GetAsync("/test2/action");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, ((InMemoryDataProvider)Configuration.DataProvider).GetAllEvents().Count);
        }
        
        [Test]
        public async Task Test3()
        {
            Configuration.Setup().UseInMemoryProvider();
            
            using var app = TestHelper.GetTestServer();
            using var client = app.CreateClient();

            var response = await client.GetAsync("/test2/action");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, ((InMemoryDataProvider)Configuration.DataProvider).GetAllEvents().Count);
        }
        
        [Test]
        public async Task Test4()
        {
            Configuration.Setup().UseInMemoryProvider();
            
            using var app = TestHelper.GetTestServer();
            using var client = app.CreateClient();

            var response = await client.GetAsync("/test2/action");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, ((InMemoryDataProvider)Configuration.DataProvider).GetAllEvents().Count);
        }
    }
}
#endif