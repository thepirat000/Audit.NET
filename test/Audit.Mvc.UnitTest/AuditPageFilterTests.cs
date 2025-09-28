#if ASP_CORE
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Microsoft.AspNetCore.Routing;

namespace Audit.Mvc.UnitTest
{
    [TestFixture]
    public class AuditPageFilterTests
    {
        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenAuditIgnoreAttributePresent()
        {
            var handlerMethodMock = new HandlerMethodDescriptor();
            handlerMethodMock.MethodInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest));

            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() }, 
                new List<IFilterMetadata>(),
                handlerMethodMock,
                new Dictionary<string, object>(),
                new TestModel()
            );
            contextMock.ActionDescriptor.HandlerTypeInfo = typeof(TestModel).GetTypeInfo();

            Assert.IsTrue(AuditPageFilter.IsActionIgnored(contextMock));
        }

        [Test]
        public async Task GetRequestBody_ReturnsBodyContent()
        {
            var bodyText = "Test Body";
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyText));
            bodyStream.Position = 0;

            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.SetupGet(r => r.Body).Returns(bodyStream);
            requestMock.SetupGet(r => r.ContentType).Returns("text/plain");
            httpContextMock.SetupGet(h => h.Request).Returns(requestMock.Object);

            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = httpContextMock.Object, RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new Dictionary<string, object>(),
                new TestModel()
            );
            contextMock.HttpContext = httpContextMock.Object;

            var result = await AuditPageFilter.GetRequestBody(contextMock, CancellationToken.None);
            Assert.AreEqual(bodyText, result);
        }

        [Test]
        public void GetModelObject_ReturnsModelDictionary()
        {
            var propMock = new PageBoundPropertyDescriptor();
            propMock.Name = "TestProp";
            propMock.Property = typeof(TestModel).GetProperty("Value");
            var handlerInstance = new TestModel { Value = "abc" };

            var actionDescriptorMock = new CompiledPageActionDescriptor();
            actionDescriptorMock.BoundProperties = new List<ParameterDescriptor> { propMock };

            var contextMock = new PageHandlerExecutedContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = actionDescriptorMock },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                handlerInstance
            );

            var result = AuditPageFilter.GetModelObject(contextMock);
            Assert.IsNotNull(result);
            Assert.AreEqual("abc", result["TestProp"]);
        }



        [Test]
        public void GetResponseBody_ReturnsExpectedValue_ForObjectResult()
        {
            var methodInfo = typeof(AuditPageFilterTests).GetMethod(nameof(GetResponseBody_ReturnsExpectedValue_ForObjectResult));
            var result = new ObjectResult("myvalue");
            var response = AuditPageFilter.GetResponseBody(methodInfo, result);
            Assert.AreEqual("myvalue", response);
        }

        [Test]
        public void GetActionParameters_FiltersIgnoredParameters()
        {
            var handlerArguments = new Dictionary<string, object>
            {
                { "param1", 123 },
                { "param2", "abc" }
            };

            var paramInfoMock1 = new HandlerParameterDescriptor();
            paramInfoMock1.Name = "param1";
            paramInfoMock1.ParameterInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)).GetParameters()[0];

            var paramInfoMock2 = new HandlerParameterDescriptor();
            paramInfoMock2.Name = "param2";
            paramInfoMock2.ParameterInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)).GetParameters()[0];

            var handlerMethodMock = new HandlerMethodDescriptor();
            handlerMethodMock.Parameters = new List<HandlerParameterDescriptor> { paramInfoMock1, paramInfoMock2 };

            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                handlerMethodMock,
                handlerArguments,
                new object()
            );

            var result = AuditPageFilter.GetActionParameters(contextMock);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(123, result["param1"]);
            Assert.AreEqual("abc", result["param2"]);
        }

        [Test]
        public async Task OnPageHandlerExecutionAsync_CallsBeforeAndAfter()
        {
            var filter = new AuditPageFilter();
            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor(){ HandlerTypeInfo = typeof(TestModel).GetTypeInfo() } },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor() { MethodInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)) },
                new Dictionary<string, object>(),
                new TestModel()
            );
            var executedContextMock = new PageHandlerExecutedContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() { HandlerTypeInfo = typeof(TestModel).GetTypeInfo() } },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new TestModel()
            );
            
            
            var nextCalled = false;
            Task<PageHandlerExecutedContext> NextDelegate()
            {
                nextCalled = true;
                return Task.FromResult(executedContextMock);
            }

            await filter.OnPageHandlerExecutionAsync(contextMock, NextDelegate);
            Assert.IsTrue(nextCalled);
        }

        [Test]
        public void OnPageHandlerSelectionAsync_ReturnsCompletedTask()
        {
            var filter = new AuditPageFilter();
            var context = new PageHandlerSelectedContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor()
            );
            var task = filter.OnPageHandlerSelectionAsync(context);
            Assert.AreEqual(Task.CompletedTask, task);
        }

        [Test]
        public async Task BeforeExecutingAsync_SetsAuditScopeAndAction_AllProperties()
        {
            var filter = new AuditPageFilter
            {
                IncludeModel = true,
                IncludeRequestBody = true,
                IncludeHeaders = true,
                IncludeResponseBody = true,
                EventTypeName = "{verb} {area} {controller} {action} {path}"
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Path = "/test";
            httpContext.Request.QueryString = new QueryString("?a=1");
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("body"));
            httpContext.Request.Headers["X-Test"] = "header";
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            httpContext.TraceIdentifier = "traceid";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "username")
            };
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                DisplayName = "TestAction",
                AreaName = "TestArea",
                ViewEnginePath = "/Pages/Test.cshtml"
            };

            var handlerMethod = new HandlerMethodDescriptor
            {
                MethodInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest))
            };

            var context = new PageHandlerExecutingContext(
                new PageContext { HttpContext = httpContext, RouteData = new RouteData(), ActionDescriptor = actionDescriptor },
                new List<IFilterMetadata>(),
                handlerMethod,
                new Dictionary<string, object>(),
                new TestModel()
            );

            await filter.BeforeExecutingAsync(context);

            Assert.IsTrue(httpContext.Items.ContainsKey("__private_AuditAction__"));
            Assert.IsTrue(httpContext.Items.ContainsKey("__private_AuditScope__"));
            var auditAction = httpContext.Items["__private_AuditAction__"] as AuditAction;
            Assert.AreEqual("username", auditAction.UserName);
            Assert.AreEqual("127.0.0.1", auditAction.IpAddress);
            Assert.AreEqual("https://localhost/test?a=1", auditAction.RequestUrl);
            Assert.AreEqual("POST", auditAction.HttpMethod);
            Assert.AreEqual("TestAction", auditAction.ActionName);
            Assert.AreEqual("TestArea", auditAction.ControllerName);
            Assert.AreEqual("/Pages/Test.cshtml", auditAction.ViewPath);
            Assert.AreEqual("traceid", auditAction.TraceId);
            Assert.IsNotNull(auditAction.Headers);
            Assert.IsNotNull(auditAction.RequestBody.Value);
        }

        [Test]
        public async Task AfterExecutedAsync_SetsAuditActionProperties_AndDisposesScope()
        {
            var filter = new AuditPageFilter
            {
                IncludeModel = true,
                IncludeResponseBody = true
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Response.StatusCode = 200;
            httpContext.Response.Headers["Location"] = "/redirect";
            httpContext.Items["__private_AuditAction__"] = new AuditAction
            {
                PageHandlerExecutingContext = new PageHandlerExecutingContext(
                    new PageContext { HttpContext = httpContext, RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                    new List<IFilterMetadata>(),
                    new HandlerMethodDescriptor { MethodInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)) },
                    new Dictionary<string, object>(),
                    new TestModel()
                )
            };
            var auditScope = AuditScope.Create(new AuditScopeOptions { DataProvider = new NullDataProvider(), AuditEvent = new AuditEventMvcAction() });
            httpContext.Items["__private_AuditScope__"] = auditScope;

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new List<ParameterDescriptor>()
            };

            var context = new PageHandlerExecutedContext(
                new PageContext { HttpContext = httpContext, RouteData = new RouteData(), ActionDescriptor = actionDescriptor },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new TestModel()
            );
            
            context.Result = new ObjectResult("result");
            context.Exception = null;

            await filter.AfterExecutedAsync(context);

            var auditAction = httpContext.Items["__private_AuditAction__"] as AuditAction;
            Assert.AreEqual("/redirect", auditAction.RedirectLocation);
            Assert.AreEqual(200, auditAction.ResponseStatusCode);
            Assert.IsNotNull(auditAction.ResponseBody);
        }

        [Test]
        public async Task AfterExecutedAsync_SetsStatusCode500_WhenExceptionAndNoResult()
        {
            var filter = new AuditPageFilter();

            var httpContext = new DefaultHttpContext();
            httpContext.Items["__private_AuditAction__"] = new AuditAction();
            httpContext.Items["__private_AuditScope__"] = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider(), AuditEvent = new AuditEventMvcAction() });

            var context = new PageHandlerExecutedContext(
                new PageContext { HttpContext = httpContext, RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new TestModel()
            );
            context.Exception = new Exception("fail");
            context.ExceptionHandled = false;
            context.Result = null;

            await filter.AfterExecutedAsync(context);

            var auditAction = httpContext.Items["__private_AuditAction__"] as AuditAction;
            Assert.AreEqual(500, auditAction.ResponseStatusCode);
        }

        [Test]
        public async Task GetRequestBody_ReturnsNull_WhenCannotRead()
        {
            var bodyStream = new MemoryStream();
            bodyStream.Close(); // Make it unreadable

            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.SetupGet(r => r.Body).Returns(bodyStream);
            httpContextMock.SetupGet(h => h.Request).Returns(requestMock.Object);

            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = httpContextMock.Object, RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new Dictionary<string, object>(),
                new TestModel()
            );
            contextMock.HttpContext = httpContextMock.Object;

            var result = await AuditPageFilter.GetRequestBody(contextMock, CancellationToken.None);
            Assert.IsNull(result);
        }

        [Test]
        public void GetModelObject_ReturnsNull_WhenNoBoundProperties()
        {
            var actionDescriptorMock = new CompiledPageActionDescriptor();
            actionDescriptorMock.BoundProperties = new List<ParameterDescriptor>();

            var contextMock = new PageHandlerExecutedContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = actionDescriptorMock },
                new List<IFilterMetadata>(),
                new HandlerMethodDescriptor(),
                new TestModel()
            );

            var result = AuditPageFilter.GetModelObject(contextMock);
            Assert.IsNull(result);
        }
        
        [Test]
        public void GetResponseBody_ReturnsExpected_ForAllResultTypes()
        {
            var methodInfo = typeof(AuditPageFilterTests).GetMethod(nameof(GetResponseBody_ReturnsExpected_ForAllResultTypes));
            Assert.AreEqual(null, AuditPageFilter.GetResponseBody(methodInfo, new PageResult()));
            Assert.AreEqual("myvalue", AuditPageFilter.GetResponseBody(methodInfo, new ObjectResult("myvalue")));
            Assert.AreEqual(204, AuditPageFilter.GetResponseBody(methodInfo, new StatusCodeResult(204)));
            Assert.AreEqual("json", AuditPageFilter.GetResponseBody(methodInfo, new JsonResult("json")));
            Assert.AreEqual("content", AuditPageFilter.GetResponseBody(methodInfo, new ContentResult { Content = "content" }));
            Assert.AreEqual("file.txt", AuditPageFilter.GetResponseBody(methodInfo, new FileContentResult(new byte[0], "text/plain") { FileDownloadName = "file.txt" }));
            Assert.AreEqual("/local", AuditPageFilter.GetResponseBody(methodInfo, new LocalRedirectResult("/local")));
            Assert.AreEqual("/redir", AuditPageFilter.GetResponseBody(methodInfo, new RedirectResult("/redir")));
            Assert.AreEqual("Action", AuditPageFilter.GetResponseBody(methodInfo, new RedirectToActionResult("Action", "Controller", null)));
            Assert.AreEqual("Route", AuditPageFilter.GetResponseBody(methodInfo, new RedirectToRouteResult("Route", null)));
            Assert.AreEqual(null, AuditPageFilter.GetResponseBody(methodInfo, new SignInResult("Scheme", new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("name", "user") }, "test")))));
            Assert.AreEqual("Partial", AuditPageFilter.GetResponseBody(methodInfo, new PartialViewResult { ViewName = "Partial" }));
            Assert.AreEqual("VC", AuditPageFilter.GetResponseBody(methodInfo, new ViewComponentResult { ViewComponentName = "VC" }));
            Assert.AreEqual("View", AuditPageFilter.GetResponseBody(methodInfo, new ViewResult { ViewName = "View" }));
            Assert.AreEqual("Page", AuditPageFilter.GetResponseBody(methodInfo, new RedirectToPageResult("Page")));
        }

        [Test]
        public void GetActionParameters_FiltersIgnoredParameter()
        {
            var handlerArguments = new Dictionary<string, object>
            {
                { "param1", 123 },
                { "param2", "abc" }
            };

            var paramInfoMock1 = new HandlerParameterDescriptor();
            paramInfoMock1.Name = "param1";
            paramInfoMock1.ParameterInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)).GetParameters()[0];
            
            var paramInfoMock2 = new HandlerParameterDescriptor();
            paramInfoMock2.Name = "param2";
            var paramInfo = typeof(TestModel).GetMethod(nameof(TestModel.ParameterTest)).GetParameters()[0];
            paramInfoMock2.ParameterInfo = paramInfo;

            var handlerMethodMock = new HandlerMethodDescriptor();
            handlerMethodMock.Parameters = new List<HandlerParameterDescriptor> { paramInfoMock1, paramInfoMock2 };
            
            var contextMock = new PageHandlerExecutingContext(
                new PageContext { HttpContext = new DefaultHttpContext(), RouteData = new RouteData(), ActionDescriptor = new CompiledPageActionDescriptor() },
                new List<IFilterMetadata>(),
                handlerMethodMock,
                handlerArguments,
                new object()
            );

            // Simulate AuditIgnore on param2
            paramInfoMock2.ParameterInfo = typeof(ParameterInfoWithAuditIgnore).GetMethod(nameof(ParameterInfoWithAuditIgnore.Get))!.GetParameters()[0];

            var result = AuditPageFilter.GetActionParameters(contextMock);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(123, result["param1"]);
        }

        public class ParameterInfoWithAuditIgnore 
        {
            public string Get([AuditIgnore]bool inherit)
            {
                return "";
            }
        }

        public class TestModel
        {
            public string Value { get; set; }

            [AuditIgnore]
            public string ParameterTest(string param)
            {
                return param ?? Value;
            }
        }
    }
}
#endif
