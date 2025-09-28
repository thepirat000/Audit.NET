#if ASP_NET
using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Audit.Core;
using Audit.Core.Providers;

namespace Audit.Mvc.UnitTest
{
    [TestFixture]
    public class AuditAttributeTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            Audit.Core.Configuration.Setup().UseNullProvider();
        }

        [Test]
        public void ToDictionary_ReturnsNull_WhenCollectionIsNull()
        {
            Assert.That(AuditAttribute.ToDictionary(null), Is.Null);
        }

        [Test]
        public void ToDictionary_ReturnsDictionary_WhenCollectionIsNotNull()
        {
            var nvc = new NameValueCollection();
            nvc.Add("A", "1");
            nvc.Add("B", "2");
            var dict = AuditAttribute.ToDictionary(nvc);
            Assert.Multiple(() =>
            {
                Assert.That(dict["A"], Is.EqualTo("1"));
                Assert.That(dict["B"], Is.EqualTo("2"));
            });
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenAuditDisabled()
        {
            var attr = new AuditAttribute();
            Audit.Core.Configuration.AuditDisabled = true;
            var actionDescMock = new Mock<ActionDescriptor>();
            var ctrlDescMock = new Mock<ControllerDescriptor>();
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);
            Assert.That(attr.IsActionIgnored(actionDescMock.Object, false), Is.True);
            Audit.Core.Configuration.AuditDisabled = false;
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenChildActionAndNotIncluded()
        {
            var attr = new AuditAttribute { IncludeChildActions = false };
            var actionDescMock = new Mock<ActionDescriptor>();
            var ctrlDescMock = new Mock<ControllerDescriptor>();
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);
            Assert.That(attr.IsActionIgnored(actionDescMock.Object, true), Is.True);
        }

        [Test]
        public void IsActionIgnored_ReturnsFalse_WhenActionDescriptorIsNull()
        {
            var attr = new AuditAttribute();
            Assert.That(attr.IsActionIgnored(null, false), Is.False);
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenControllerHasAuditIgnore()
        {
            var attr = new AuditAttribute();
            var ctrlType = typeof(TestControllerWithIgnore);
            var ctrlDescMock = new Mock<ControllerDescriptor>();
            ctrlDescMock.SetupGet(c => c.ControllerType).Returns(ctrlType);
            var actionDescMock = new Mock<ActionDescriptor>();
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);
            Assert.That(attr.IsActionIgnored(actionDescMock.Object, false), Is.True);
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenActionHasAuditIgnore()
        {
            var attr = new AuditAttribute();
            var ctrlType = typeof(TestController);
            var ctrlDescMock = new Mock<ControllerDescriptor>();
            ctrlDescMock.SetupGet(c => c.ControllerType).Returns(ctrlType);
            var actionDescMock = new Mock<ActionDescriptor>();
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);
            actionDescMock.Setup(a => a.GetCustomAttributes(typeof(AuditIgnoreAttribute), true)).Returns(new object[] { new AuditIgnoreAttribute() });
            Assert.That(attr.IsActionIgnored(actionDescMock.Object, false), Is.True);
        }

        [Test]
        public void GetActionParameters_FiltersIgnoredParameters_AndSerializes()
        {
            var attr = new AuditAttribute { SerializeActionParameters = true };
            var actionDescMock = new Mock<ActionDescriptor>();
            var paramMock1 = new Mock<ParameterDescriptor>();
            paramMock1.SetupGet(p => p.ParameterName).Returns("p1");
            paramMock1.Setup(p => p.GetCustomAttributes(typeof(AuditIgnoreAttribute), true)).Returns(new object[0]);
            var paramMock2 = new Mock<ParameterDescriptor>();
            paramMock2.SetupGet(p => p.ParameterName).Returns("p2");
            paramMock2.Setup(p => p.GetCustomAttributes(typeof(AuditIgnoreAttribute), true)).Returns(new object[] { new AuditIgnoreAttribute() });
            actionDescMock.Setup(a => a.GetParameters()).Returns(new[] { paramMock1.Object, paramMock2.Object });

            var contextMock = new Mock<ActionExecutingContext>();
            contextMock.SetupGet(c => c.ActionDescriptor).Returns(actionDescMock.Object);
            contextMock.SetupGet(c => c.ActionParameters).Returns(new Dictionary<string, object> { { "p1", 1 }, { "p2", 2 } });

            var result = attr.GetActionParameters(contextMock.Object);
            Assert.Multiple(() =>
            {
                Assert.That(result.ContainsKey("p1"), Is.True);
                Assert.That(result.ContainsKey("p2"), Is.False);
            });
        }

        [Test]
        public void GetActionParameters_ReturnsRaw_WhenNotSerialized()
        {
            var attr = new AuditAttribute { SerializeActionParameters = false };
            var actionDescMock = new Mock<ActionDescriptor>();
            var paramMock1 = new Mock<ParameterDescriptor>();
            paramMock1.SetupGet(p => p.ParameterName).Returns("p1");
            paramMock1.Setup(p => p.GetCustomAttributes(typeof(AuditIgnoreAttribute), true)).Returns(new object[0]);
            actionDescMock.Setup(a => a.GetParameters()).Returns(new[] { paramMock1.Object });

            var contextMock = new Mock<ActionExecutingContext>();
            contextMock.SetupGet(c => c.ActionDescriptor).Returns(actionDescMock.Object);
            contextMock.SetupGet(c => c.ActionParameters).Returns(new Dictionary<string, object> { { "p1", 1 } });

            var result = attr.GetActionParameters(contextMock.Object);
            Assert.That(result["p1"], Is.EqualTo(1));
        }

        [Test]
        public void GetRequestBody_ReturnsNull_WhenNoInputStream()
        {
            var attr = new TestAuditAttribute();
            var httpContextMock = new Mock<HttpContextBase>();
            httpContextMock.SetupGet(h => h.Request).Returns((HttpRequestBase)null);
            Assert.That(attr.GetRequestBody(httpContextMock.Object), Is.Null);
        }

        [Test]
        public void GetRequestBody_ReturnsBodyContent_WhenInputStreamPresent()
        {
            var attr = new TestAuditAttribute();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("body"));
            var reqMock = new Mock<HttpRequestBase>();
            reqMock.SetupGet(r => r.InputStream).Returns(ms);
            reqMock.SetupGet(r => r.ContentType).Returns("text/plain");
            reqMock.SetupGet(r => r.ContentLength).Returns(4);
            var httpContextMock = new Mock<HttpContextBase>();
            httpContextMock.SetupGet(h => h.Request).Returns(reqMock.Object);

            var result = attr.GetRequestBody(httpContextMock.Object);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.EqualTo("body"));
                Assert.That(result.Type, Is.EqualTo("text/plain"));
                Assert.That(result.Length, Is.EqualTo(4));
            });
        }

        [Test]
        public void GetResponseBody_ReturnsExpected_ForAllResultTypes()
        {
            var attr = new AuditAttribute();
            Assert.Multiple(() =>
            {
                Assert.That(attr.GetResponseBody(new EmptyResult()).Value, Is.EqualTo(""));
                Assert.That(attr.GetResponseBody(new FileContentResult(new byte[0], "text/plain") { FileDownloadName = "file.txt" }).Value, Is.EqualTo("file.txt"));
                Assert.That(attr.GetResponseBody(new JsonResult { Data = "data" }).Value, Is.EqualTo("data"));
                Assert.That(attr.GetResponseBody(new JavaScriptResult { Script = "script" }).Value, Is.EqualTo("script"));
                Assert.That(attr.GetResponseBody(new ContentResult { Content = "content", ContentType = "text/plain" }).Value, Is.EqualTo("content"));
                Assert.That(attr.GetResponseBody(new RedirectResult("/redir")).Value, Is.EqualTo("/redir"));
                Assert.That(attr.GetResponseBody(new RedirectToRouteResult("route", null)).Value, Is.EqualTo("route"));
                Assert.That(attr.GetResponseBody(new PartialViewResult { ViewName = "partial" }).Value, Is.EqualTo("partial"));
                Assert.That(attr.GetResponseBody(new ViewResult { ViewName = "view" }).Value, Is.EqualTo("view"));
                Assert.That(attr.GetResponseBody(new TestViewResultBase { ViewName = "view" }).Value, Is.EqualTo("view"));
                Assert.That(attr.GetResponseBody(new TestActionResult()).Value, Is.EqualTo("overriden"));
            });
        }

        [Test]
        public void GetCurrentScope_ReturnsScope_WhenPresent()
        {
            var httpContextMock = new Mock<HttpContextBase>();
            var items = new Dictionary<string, object>();
            var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });
            items["__private_AuditScope__"] = scope;
            httpContextMock.SetupGet(h => h.Items).Returns(items);
            Assert.That(AuditAttribute.GetCurrentScope(httpContextMock.Object), Is.EqualTo(scope));
        }

        [Test]
        public void GetCurrentScope_ReturnsNull_WhenNotPresent()
        {
            var httpContextMock = new Mock<HttpContextBase>();
            httpContextMock.SetupGet(h => h.Items).Returns(new Dictionary<string, object>());
            Assert.That(AuditAttribute.GetCurrentScope(httpContextMock.Object), Is.Null);
        }

        [Test]
        public void OnActionExecuting_SetsAuditScopeAndAction()
        {
            var attr = new AuditAttribute();
            var httpContextMock = new Mock<HttpContextBase>();
            var items = new Dictionary<string, object>();
            httpContextMock.SetupGet(h => h.Items).Returns(items);

            var reqMock = new Mock<HttpRequestBase>();
            reqMock.SetupGet(r => r.IsAuthenticated).Returns(true);
            reqMock.SetupGet(r => r.ContentType).Returns("text/plain");
            reqMock.SetupGet(r => r.ContentLength).Returns(0);
            reqMock.SetupGet(r => r.ServerVariables).Returns(new NameValueCollection());
            reqMock.SetupGet(r => r.UserHostAddress).Returns("127.0.0.1");
            reqMock.SetupGet(r => r.RawUrl).Returns("/test");
            reqMock.SetupGet(r => r.Form).Returns(new NameValueCollection());
            reqMock.SetupGet(r => r.Headers).Returns(new NameValueCollection());
            reqMock.SetupGet(r => r.HttpMethod).Returns("GET");
            httpContextMock.SetupGet(h => h.Request).Returns(reqMock.Object);

            var userMock = new Mock<System.Security.Principal.IPrincipal>();
            var identityMock = new Mock<System.Security.Principal.IIdentity>();
            identityMock.SetupGet(i => i.Name).Returns("user");
            userMock.SetupGet(u => u.Identity).Returns(identityMock.Object);
            httpContextMock.SetupGet(h => h.User).Returns(userMock.Object);

            var ctrlDescMock = new Mock<ControllerDescriptor>();
            ctrlDescMock.SetupGet(c => c.ControllerName).Returns("TestCtrl");
            ctrlDescMock.SetupGet(c => c.ControllerType).Returns(typeof(TestController));
            var actionDescMock = new Mock<ActionDescriptor>();
            actionDescMock.SetupGet(a => a.ActionName).Returns("TestAction");
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);

            var context = new ActionExecutingContext
            {
                HttpContext = httpContextMock.Object,
                ActionDescriptor = actionDescMock.Object,
                ActionParameters = new Dictionary<string, object>()
            };

            attr.OnActionExecuting(context);

            Assert.Multiple(() =>
            {
                Assert.That(items.ContainsKey("__private_AuditAction__"), Is.True);
                Assert.That(items.ContainsKey("__private_AuditScope__"), Is.True);
            });
        }

        [Test]
        public void OnActionExecuted_SetsAuditActionProperties_AndSavesScopeOnException()
        {
            var attr = new AuditAttribute { IncludeModel = true, IncludeResponseBody = true };
            var httpContextMock = new Mock<HttpContextBase>();
            var items = new Dictionary<string, object>();
            var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider(), CreationPolicy = EventCreationPolicy.Manual, AuditEvent = new AuditEventMvcAction() });
            items["__private_AuditAction__"] = new AuditAction();
            items["__private_AuditScope__"] = scope;
            httpContextMock.SetupGet(h => h.Items).Returns(items);

            var ctrlMock = new TestController();
            var viewData = new ViewDataDictionary();
            viewData.Model = "model";
            ctrlMock.ViewData = viewData;

            var ctrlDescMock = new Mock<ControllerDescriptor>();
            ctrlDescMock.SetupGet(c => c.ControllerName).Returns("TestCtrl");
            ctrlDescMock.SetupGet(c => c.ControllerType).Returns(typeof(TestController));

            var actionDescMock = new Mock<ActionDescriptor>();
            actionDescMock.SetupGet(a => a.ActionName).Returns("TestAction");
            actionDescMock.SetupGet(a => a.ControllerDescriptor).Returns(ctrlDescMock.Object);

            var context = new ActionExecutedContext
            {
                HttpContext = httpContextMock.Object,
                Controller = ctrlMock,
                ActionDescriptor = actionDescMock.Object,
                Exception = new System.Exception("fail"),
                Result = new ContentResult { Content = "content" }
            };

            attr.OnActionExecuted(context);

            var auditAction = items["__private_AuditAction__"] as AuditAction;
            Assert.Multiple(() =>
            {
                Assert.That(auditAction, Is.Not.Null);
                Assert.That(auditAction.Model, Is.EqualTo("model"));
                Assert.That(auditAction.Exception, Does.Contain("fail"));
                Assert.That(auditAction.ResponseBody, Is.Not.Null);
            });
        }

        [Test]
        public void OnResultExecuted_SetsAuditActionProperties_AndDisposesScope()
        {
            var attr = new AuditAttribute { IncludeResponseBody = true };
            var httpContextMock = new Mock<HttpContextBase>();
            var items = new Dictionary<string, object>();
            var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider(), CreationPolicy = EventCreationPolicy.Manual, AuditEvent = new AuditEventMvcAction() });
            items["__private_AuditAction__"] = new AuditAction();
            items["__private_AuditScope__"] = scope;
            httpContextMock.SetupGet(h => h.Items).Returns(items);

            var respMock = new Mock<HttpResponseBase>();
            respMock.SetupGet(r => r.RedirectLocation).Returns("/redir");
            respMock.SetupGet(r => r.Status).Returns("OK");
            respMock.SetupGet(r => r.StatusCode).Returns(200);
            httpContextMock.SetupGet(h => h.Response).Returns(respMock.Object);

            var viewResult = new ViewResult { ViewName = "view" };
            var context = new ResultExecutedContext
            {
                HttpContext = httpContextMock.Object,
                Result = viewResult
            };

            attr.OnResultExecuted(context);

            var auditAction = items["__private_AuditAction__"] as AuditAction;
            Assert.Multiple(() =>
            {
                Assert.That(auditAction.RedirectLocation, Is.EqualTo("/redir"));
                Assert.That(auditAction.ResponseStatus, Is.EqualTo("OK"));
                Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(200));
                Assert.That(auditAction.ViewName, Is.EqualTo("view"));
                Assert.That(auditAction.ResponseBody, Is.Not.Null);
            });
        }

        [Test]
        public void OnResultExecuted_ChildActionIgnored_DoesNothing()
        {
            var attr = new AuditAttribute { IncludeResponseBody = true, IncludeChildActions = false };
            var httpContextMock = new Mock<HttpContextBase>();
            var items = new Dictionary<string, object>();
            var dp = new InMemoryDataProvider();
            var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp, CreationPolicy = EventCreationPolicy.Manual, AuditEvent = new AuditEventMvcAction() });
            items["__private_AuditAction__"] = new AuditAction();
            items["__private_AuditScope__"] = scope;
            httpContextMock.SetupGet(h => h.Items).Returns(items);

            var respMock = new Mock<HttpResponseBase>();
            respMock.SetupGet(r => r.RedirectLocation).Returns("/redir");
            respMock.SetupGet(r => r.Status).Returns("OK");
            respMock.SetupGet(r => r.StatusCode).Returns(200);
            httpContextMock.SetupGet(h => h.Response).Returns(respMock.Object);

            var viewResult = new ViewResult { ViewName = "view" };
            var routeData = new RouteData();
            routeData.DataTokens.Add("ParentActionViewContext", new ResultExecutedContext());
            var context = new ResultExecutedContext
            {
                HttpContext = httpContextMock.Object,
                Result = viewResult,
                RouteData = routeData
            };
            
            attr.OnResultExecuted(context);

            Assert.That(dp.GetAllEvents(), Has.Count.EqualTo(0));
        }

        // Helper types
        public class TestController : Controller { }
        [AuditIgnore]
        public class TestControllerWithIgnore : Controller { }
        public class TestViewResultBase : ViewResultBase
        {
            public override void ExecuteResult(ControllerContext context) { }
            protected override ViewEngineResult FindView(ControllerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class TestActionResult : ActionResult
        {
            public override void ExecuteResult(ControllerContext context)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return "overriden";
            }
        }

        public class TestAuditAttribute : AuditAttribute
        {
            public new BodyContent GetRequestBody(HttpContextBase context)
            {
                return base.GetRequestBody(context);
            }
        }
    }
}
#endif