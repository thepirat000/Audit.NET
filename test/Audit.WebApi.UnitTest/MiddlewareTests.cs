#if NETCOREAPP3_1 || NET6_0
using System.Net;
using System.Threading.Tasks;
using Audit.Core.Providers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Audit.WebApi.UnitTest
{
    [Route("[controller]")]
    [ApiController]
    public class TestMiddlewareController : ControllerBase
    {
        [Route("")]
        [HttpGet]
        public IActionResult Get([FromQuery] int length)
        {
            return Ok(new string('#', length));
        }
    }

    [Parallelizable]
    public class MiddlewareTests
    {
        [TestCase(null, null, true, true)]
        [TestCase(null, false, false, false)]
        [TestCase(null, true, false, true)]
        [TestCase(true, null, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, true)]
        [TestCase(false, null, true, true)]
        [TestCase(false, false, true, true)]
        [TestCase(false, true, true, true)]
        public async Task Test_IncludeResponseBody(bool? includeResponseBody, bool? skipResponseBody, bool expectNullResponseBody, bool expectNullResponseBodyContent)
        {
            var dataProvider = new InMemoryDataProvider();
            
            using var app = TestHelper.GetTestServer(dataProvider, cfg =>
            {
                cfg.FilterByRequest(r => r.Path.Value?.Contains("TestMiddleware") == true);
                if (includeResponseBody.HasValue)
                {
                    cfg.IncludeResponseBody(includeResponseBody!.Value);
                }
                if (skipResponseBody.HasValue)
                {
                    if (skipResponseBody.GetValueOrDefault())
                    {
                        cfg.SkipResponseBodyContent(_ => skipResponseBody!.Value);
                    }
                    else
                    {
                        cfg.SkipResponseBodyContent(skipResponseBody!.Value);
                    }
                }
            });

            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestMiddleware?length=10");

            var events = dataProvider.GetAllEvents();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].GetWebApiAuditAction().ResponseBody, expectNullResponseBody ? Is.Null : Is.Not.Null);
            Assert.That(events[0].GetWebApiAuditAction().ResponseBody?.Value, expectNullResponseBodyContent ? Is.Null : Is.Not.Null);
        }
    }
}
#endif