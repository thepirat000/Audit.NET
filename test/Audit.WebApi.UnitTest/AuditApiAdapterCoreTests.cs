#if ASP_CORE
using Audit.Core;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

using NUnit.Framework;

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

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

        [TestCase(true, false)]
        [TestCase(false, true)]
        public async Task Test_BeforeExecutingAsync_IncludeRequestBody_ShouldControlFormVariables(bool includeRequestBody, bool expectNullFormVariables)
        {
            // Arrange
            var adapter = new AuditApiAdapter();
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor
            {
                ControllerName = "Test",
                ActionName = "Action",
                MethodInfo = typeof(AuditApiAdapterCoreTests).GetMethod(nameof(GetClaimsPrincipal))!,
                Parameters = new List<Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor>()
            });

            // Setup HTTP request properties
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Path = "/test/action";
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Method = "POST";

            // Setup form data
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            });
            httpContext.Request.Form = formCollection;

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object());

            // Act
            await adapter.BeforeExecutingAsync(actionExecutingContext, includeHeaders: false, includeRequestBody, serializeParams: true, eventTypeName: null);

            // Assert
            var auditAction = httpContext.Items[AuditApiHelper.AuditApiActionKey] as AuditApiAction;
            Assert.That(auditAction, Is.Not.Null);

            if (expectNullFormVariables)
            {
                Assert.That(auditAction.FormVariables, Is.Null, "FormVariables should be null when IncludeRequestBody is false");
            }
            else
            {
                Assert.That(auditAction.FormVariables, Is.Not.Null, "FormVariables should not be null when IncludeRequestBody is true");
                Assert.That(auditAction.FormVariables.ContainsKey("key1"), Is.True);
                Assert.That(auditAction.FormVariables["key1"], Is.EqualTo("value1"));
                Assert.That(auditAction.FormVariables.ContainsKey("key2"), Is.True);
                Assert.That(auditAction.FormVariables["key2"], Is.EqualTo("value2"));
            }

            // Cleanup
            var auditScope = httpContext.Items[AuditApiHelper.AuditApiScopeKey] as IAuditScope;
            if (auditScope != null)
            {
                await auditScope.DisposeAsync();
            }
        }
    }
}
#endif

