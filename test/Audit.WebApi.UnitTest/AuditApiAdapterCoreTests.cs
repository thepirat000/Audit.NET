#if ASP_CORE
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using NUnit.Framework;

using System.Collections.Generic;
using System.Security.Claims;

namespace Audit.WebApi.UnitTest
{
    [TestFixture]
    public class AuditApiAdapterCoreTests
    {
        [Test]
        public void Test_AuditApiAdapterCore_GetResponseBody()
        {
            // Arrange & Act
            var bodyObject = AuditApiAdapter.GetResponseBody(null, new ObjectResult("test"));
            var bodyStatusCode = AuditApiAdapter.GetResponseBody(null, new StatusCodeResult(500));
            var bodyJson = AuditApiAdapter.GetResponseBody(null, new JsonResult(new { Id = 1 }));
            var bodyContent = AuditApiAdapter.GetResponseBody(null, new ContentResult() { Content = "content" });
            var bodyFile =
                AuditApiAdapter.GetResponseBody(null, new FileContentResult([1, 2, 3], "application/octet-stream") { FileDownloadName = "download" });
            var bodyLocalRedirect = AuditApiAdapter.GetResponseBody(null, new LocalRedirectResult("/test"));
            var bodyRedirect = AuditApiAdapter.GetResponseBody(null, new RedirectResult("/redirect"));
            var bodyRedirectToAction =
                AuditApiAdapter.GetResponseBody(null, new RedirectToActionResult("action", "controller", null));
            var bodyRedirectToRoute = AuditApiAdapter.GetResponseBody(null, new RedirectToRouteResult("route", null));
#if NET6_0_OR_GREATER
            var bodySignIn = AuditApiAdapter.GetResponseBody(null, new SignInResult(GetClaimsPrincipal(), new AuthenticationProperties()));
#else
            var bodySignIn = AuditApiAdapter.GetResponseBody(null, new SignInResult("a", GetClaimsPrincipal()));
#endif
            var bodyPartialView = AuditApiAdapter.GetResponseBody(null, new PartialViewResult() { ViewName = "view" });
            var bodyViewComponent = AuditApiAdapter.GetResponseBody(null, new ViewComponentResult() { ViewComponentName = "component" });
            var bodyView = AuditApiAdapter.GetResponseBody(null, new ViewResult() { ViewName = "view" });
            var bodyRedirectToPage = AuditApiAdapter.GetResponseBody(null, new RedirectToPageResult("page"));
            
            var ar = new SignOutResult("a", new AuthenticationProperties());
            var bodyToString = AuditApiAdapter.GetResponseBody(null, ar);

            // Assert
            Assert.That(bodyObject, Is.EqualTo("test"));
            Assert.That(bodyStatusCode, Is.EqualTo(500));
            Assert.That(((dynamic)bodyJson).Id, Is.EqualTo(1));
            Assert.That(bodyContent, Is.EqualTo("content"));
            Assert.That(bodyFile, Is.EqualTo("download"));
            Assert.That(bodyLocalRedirect, Is.EqualTo("/test"));
            Assert.That(bodyRedirect, Is.EqualTo("/redirect"));
            Assert.That(bodyRedirectToAction, Is.EqualTo("action"));
            Assert.That(bodyRedirectToRoute, Is.EqualTo("route"));
            Assert.That(bodySignIn, Is.EqualTo("test-user"));
            Assert.That(bodyPartialView, Is.EqualTo("view"));
            Assert.That(bodyViewComponent, Is.EqualTo("component"));
            Assert.That(bodyView, Is.EqualTo("view"));
            Assert.That(bodyRedirectToPage, Is.EqualTo("page"));
            Assert.That(bodyToString, Is.EqualTo(ar.ToString()));
        }

        public static ClaimsPrincipal GetClaimsPrincipal(string userId = "123", string userName = "test-user", string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
    }
}
#endif