#if NETCOREAPP3_1 || NET6_0_OR_GREATER
using System.Net;
using System.Threading.Tasks;
using Audit.Core;
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

        [Route("form")]
        [HttpPost]
        public IActionResult PostForm([FromForm] string key, [FromForm] string value)
        {
            return Ok(new { Key = key, Value = value });
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
            }, new CustomAuditScopeFactory());

            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestMiddleware?length=10");

            var events = dataProvider.GetAllEvents();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].CustomFields, Contains.Key("TestField"));
            Assert.That(events[0].CustomFields["TestField"].ToString(), Is.EqualTo("FromOnConfiguring"));
            Assert.That(events[0].CustomFields, Contains.Key("TestField2"));
            Assert.That(events[0].CustomFields["TestField2"].ToString(), Is.EqualTo("FromOnScopeCreated"));
            Assert.That(events[0].GetWebApiAuditAction().ResponseBody, expectNullResponseBody ? Is.Null : Is.Not.Null);
            Assert.That(events[0].GetWebApiAuditAction().ResponseBody?.Value, expectNullResponseBodyContent ? Is.Null : Is.Not.Null);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public async Task Test_IncludeRequestBody_ShouldControlFormVariables(bool includeRequestBody, bool expectNullFormVariables)
        {
            var dataProvider = new InMemoryDataProvider();

            using var app = TestHelper.GetTestServer(dataProvider, cfg =>
            {
                cfg.FilterByRequest(r => r.Path.Value?.Contains("TestMiddleware") == true);
                cfg.IncludeRequestBody(includeRequestBody);
            });

            using var client = app.CreateClient();

            var formContent = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("key", "testKey"),
                new System.Collections.Generic.KeyValuePair<string, string>("value", "testValue")
            });

            var response = await client.PostAsync("/TestMiddleware/form", formContent);

            var events = dataProvider.GetAllEvents();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(events, Has.Count.EqualTo(1));

            var auditAction = events[0].GetWebApiAuditAction();

            if (expectNullFormVariables)
            {
                Assert.That(auditAction.FormVariables, Is.Null, "FormVariables should be null when IncludeRequestBody is false");
            }
            else
            {
                Assert.That(auditAction.FormVariables, Is.Not.Null, "FormVariables should not be null when IncludeRequestBody is true");
                Assert.That(auditAction.FormVariables.ContainsKey("key"), Is.True);
                Assert.That(auditAction.FormVariables["key"], Is.EqualTo("testKey"));
                Assert.That(auditAction.FormVariables.ContainsKey("value"), Is.True);
                Assert.That(auditAction.FormVariables["value"], Is.EqualTo("testValue"));
            }
        }
    }

    public class CustomAuditScopeFactory : AuditScopeFactory
    {
        public override void OnConfiguring(AuditScopeOptions options)
        {
            options.Items.Add("TestItem", "TestValue");
            options.ExtraFields = new { TestField = "FromOnConfiguring" };
        }

        public override void OnScopeCreated(AuditScope auditScope)
        {
            if (auditScope.Items["TestItem"].ToString() != "TestValue")
            {
                Assert.Fail("TestItem not found");
            }
            auditScope.SetCustomField("TestField2", "FromOnScopeCreated");
        }
    }
}
#endif