#if ASP_NET
using Audit.Core;
using Audit.Core.Providers;

using Moq;

using NUnit.Framework;

using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace Audit.Mvc.UnitTest
{
    
    [TestFixture]
    public class ControllerExtensionsTests
    {
        private const string AuditScopeKey = "__private_AuditScope__";

        private class MyController : Controller
        {
        }

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }
        
        [Test]
        public void Test_GetCurrentAuditScope_Controller_Extension()
        {
            // Arrange
            using var expectedScope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });
            var controller = new MyController();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.Setup(h => h.Items).Returns(new Dictionary<string, object>
            {
                { AuditScopeKey, expectedScope }
            });
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

            // Act
            var auditScope = controller.GetCurrentAuditScope();

            // Assert
            Assert.That(auditScope, Is.SameAs(expectedScope));
        }

        [Test]
        public void Test_GetCurrentAuditScope_HttpContext_Extension()
        {
            // Arrange
            using var expectedScope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });

            var httpContext = new Mock<HttpContextBase>();
            
            httpContext.Setup(h => h.Items).Returns(new Dictionary<string, object>
            {
                { AuditScopeKey, expectedScope }
            });

            // Act
            var auditScope = httpContext.Object.GetCurrentAuditScope();

            // Assert
            Assert.That(auditScope, Is.SameAs(expectedScope));
        }
    }
}
#endif