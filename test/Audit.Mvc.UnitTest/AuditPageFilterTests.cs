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

            Assert.That(AuditPageFilter.IsActionIgnored(contextMock), Is.True);
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
            Assert.That(result, Is.EqualTo(bodyText));
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
            Assert.That(result, Is.Not.Null);
            Assert.That(result["TestProp"], Is.EqualTo("abc"));
        }



        [Test]
        public void GetResponseBody_ReturnsExpectedValue_ForObjectResult()
        {
            var methodInfo = typeof(AuditPageFilterTests).GetMethod(nameof(GetResponseBody_ReturnsExpectedValue_ForObjectResult));
            var result = new ObjectResult("myvalue");
            var response = AuditPageFilter.GetResponseBody(methodInfo, result);
            Assert.That(response, Is.EqualTo("myvalue"));
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
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["param1"], Is.EqualTo(123));
            Assert.That(result["param2"], Is.EqualTo("abc"));
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
            Assert.That(nextCalled, Is.True);
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
            Assert.That(task, Is.EqualTo(Task.CompletedTask));
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

            Assert.That(httpContext.Items.ContainsKey("__private_AuditAction__"), Is.True);
            Assert.That(httpContext.Items.ContainsKey("__private_AuditScope__"), Is.True);
            var auditAction = httpContext.Items["__private_AuditAction__"] as AuditAction;
            Assert.That(auditAction.UserName, Is.EqualTo("username"));
            Assert.That(auditAction.IpAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(auditAction.RequestUrl, Is.EqualTo("https://localhost/test?a=1"));
            Assert.That(auditAction.HttpMethod, Is.EqualTo("POST"));
            Assert.That(auditAction.ActionName, Is.EqualTo("TestAction"));
            Assert.That(auditAction.ControllerName, Is.EqualTo("TestArea"));
            Assert.That(auditAction.ViewPath, Is.EqualTo("/Pages/Test.cshtml"));
            Assert.That(auditAction.TraceId, Is.EqualTo("traceid"));
            Assert.That(auditAction.Headers, Is.Not.Null);
            Assert.That(auditAction.RequestBody.Value, Is.Not.Null);
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
            Assert.That(auditAction.RedirectLocation, Is.EqualTo("/redirect"));
            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(auditAction.ResponseBody, Is.Not.Null);
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
            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(500));
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
            Assert.That(result, Is.Null);
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
            Assert.That(result, Is.Null);
        }
        
        [Test]
        public void GetResponseBody_ReturnsExpected_ForAllResultTypes()
        {
            var methodInfo = typeof(AuditPageFilterTests).GetMethod(nameof(GetResponseBody_ReturnsExpected_ForAllResultTypes));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new PageResult()), Is.EqualTo(null));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new ObjectResult("myvalue")), Is.EqualTo("myvalue"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new StatusCodeResult(204)), Is.EqualTo(204));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new JsonResult("json")), Is.EqualTo("json"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new ContentResult { Content = "content" }), Is.EqualTo("content"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new FileContentResult(new byte[0], "text/plain") { FileDownloadName = "file.txt" }), Is.EqualTo("file.txt"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new LocalRedirectResult("/local")), Is.EqualTo("/local"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new RedirectResult("/redir")), Is.EqualTo("/redir"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new RedirectToActionResult("Action", "Controller", null)), Is.EqualTo("Action"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new RedirectToRouteResult("Route", null)), Is.EqualTo("Route"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new SignInResult("Scheme", new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("name", "user") }, "test")))), Is.EqualTo(null));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new PartialViewResult { ViewName = "Partial" }), Is.EqualTo("Partial"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new ViewComponentResult { ViewComponentName = "VC" }), Is.EqualTo("VC"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new ViewResult { ViewName = "View" }), Is.EqualTo("View"));
            Assert.That(AuditPageFilter.GetResponseBody(methodInfo, new RedirectToPageResult("Page")), Is.EqualTo("Page"));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result["param1"], Is.EqualTo(123));
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
