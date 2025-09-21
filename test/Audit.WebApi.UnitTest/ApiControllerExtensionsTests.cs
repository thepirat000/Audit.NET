using System.Collections.Generic;
using System.Linq;

using Audit.Core.Providers;

using Moq;

using Audit.Core;

using System.Net.Http;


#if ASP_CORE
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
#endif
using NUnit.Framework;

namespace Audit.WebApi.UnitTest
{
    [TestFixture]
    public class ApiControllerExtensionsTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

#if ASP_CORE
        [Test]
        public void AddAuditFilter()
        {
            var mvcOptions = new MvcOptions();

            mvcOptions.AddAuditFilter(cfg => cfg.LogAllActions().WithEventType(_ => "Test"));

            Assert.That(mvcOptions.Filters, Has.Count.EqualTo(1));
            Assert.That(mvcOptions.Filters[0], Is.TypeOf<AuditApiGlobalFilter>());
        }

        [Test]
        public void GetCurrentAuditScope()
        {
            var auditScope = AuditScope.Create(c => c.DataProvider(new NullDataProvider()));

            var httpContext = new Mock<HttpContext>(MockBehavior.Loose);
            httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>()
            {
                [AuditApiHelper.AuditApiScopeKey] = auditScope
            });

            var returnedScope = httpContext.Object.GetCurrentAuditScope();

            Assert.That(returnedScope, Is.SameAs(auditScope));
        }

        [Test]
        public void DiscardCurrentAuditScope()
        {
            var dp = new InMemoryDataProvider();
            var auditScope = AuditScope.Create(c => c.DataProvider(dp).CreationPolicy(EventCreationPolicy.Manual));

            var httpContext = new Mock<HttpContext>(MockBehavior.Loose);
            httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>()
            {
                [AuditApiHelper.AuditApiScopeKey] = auditScope
            });

            httpContext.Object.DiscardCurrentAuditScope();

            auditScope.Save();

            var events = dp.GetAllEvents();

            Assert.That(events, Is.Empty);
        }
#else
        [Test]
        public void AddAuditFilter()
        {
            var mvcOptions = new System.Web.Http.HttpConfiguration();

            mvcOptions.AddAuditFilter(cfg => cfg.LogAllActions().WithEventType("Test"));

            Assert.That(mvcOptions.Filters, Has.Count.EqualTo(1));
            Assert.That(mvcOptions.Filters.First().Instance, Is.TypeOf<AuditApiGlobalFilter>());
        }

        [Test]
        public void GetCurrentAuditScope()
        {
            var httpRequestMessage = new Mock<HttpRequestMessage>(MockBehavior.Loose);

            var returnedScope = httpRequestMessage.Object.GetCurrentAuditScope();

            Assert.That(returnedScope, Is.Null);
        }
#endif

    }
}
