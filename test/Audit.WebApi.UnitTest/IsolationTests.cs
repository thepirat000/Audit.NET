﻿#if NETCOREAPP3_1 || NET6_0
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Audit.Core.Providers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

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
            using var app = TestHelper.GetTestServer(dataProvider, 
                cfg => cfg.FilterByRequest(r => r.Path.Value?.Contains("Action_Middleware") == true));
            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestIsolation/Action_AuditApiAttribute?q={guid}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(dataProvider.GetAllEvents().Count, Is.EqualTo(1));
            Assert.That(dataProvider.GetAllEventsOfType<AuditEventWebApi>().First().Action.ActionParameters["q"].ToString(), Is.EqualTo(guid.ToString()));
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
            using var app = TestHelper.GetTestServer(dataProvider, cfg => cfg
                .FilterByRequest(r => r.Path.Value?.Contains("Action_Middleware") == true));
            using var client = app.CreateClient();

            var response = await client.GetAsync($"/TestIsolation/Action_Middleware?q={guid}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(dataProvider.GetAllEvents().Count, Is.EqualTo(1));
            Assert.That(dataProvider.GetAllEventsOfType<AuditEventWebApi>().First().Action.RequestUrl.Contains(guid.ToString()), Is.True);
        }
    }
}
#endif