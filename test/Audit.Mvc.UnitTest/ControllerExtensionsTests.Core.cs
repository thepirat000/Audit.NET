#if ASP_CORE
using Audit.Core;
using Audit.Core.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Moq;

using NUnit.Framework;

using System.Collections.Generic;
using System.Web;

namespace Audit.Mvc.UnitTest
{
    
    [TestFixture]
    public class ControllerExtensionsCoreTests
    {
        private const string AuditScopeKey = "__private_AuditScope__";

        private class MyController : ControllerBase { }
        private class MyPageModel : PageModel { }

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }
        
        [Test]
        public void Test_GetCurrentAuditScope_Controller_ControllerBase_Extension()
        {
            // Arrange
            using var expectedScope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });

            var httpContext = new Mock<HttpContext>();

            httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>
            {
                { AuditScopeKey, expectedScope }
            });

            var controller = new MyController
            {
                ControllerContext = new ControllerContext() { HttpContext = httpContext.Object }
            };

            // Act
            var auditScope = controller.GetCurrentAuditScope();

            // Assert
            Assert.That(auditScope, Is.SameAs(expectedScope));
        }

        [Test]
        public void Test_GetCurrentAuditScope_Controller_PageModel_Extension()
        {
            // Arrange
            using var expectedScope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });

            var httpContext = new Mock<HttpContext>();

            httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>
            {
                { AuditScopeKey, expectedScope }
            });

            var pageModel = new MyPageModel()
            {
                PageContext = new PageContext()
                {
                    HttpContext = httpContext.Object
                }
            };

            // Act
            var auditScope = pageModel.GetCurrentAuditScope();

            // Assert
            Assert.That(auditScope, Is.SameAs(expectedScope));
        }
    }
}
#endif