#if ASP_CORE
using System.Collections.Generic;
using Moq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.Controllers;
using Audit.Core;
using System;
using System.Threading.Tasks;
using Audit.Core.Providers;
using NUnit.Framework;
using System.Net;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Reflection;
using System.Linq;
using System.Threading;

namespace Audit.WebApi.UnitTest
{
    public class ActionFilterUnitTest
    {
        [Test]
        public void Test_AuditApiActionFilter_ShouldIncludeResponseBody()
        {
            var testCases = new List<TestExcludeCase>()
            {
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = null, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = null, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = true },

                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new [] { HttpStatusCode.OK }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = false },
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new [] { HttpStatusCode.OK }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = false },

                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = true },

                new TestExcludeCase { ExcludeList = null, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = null, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = true },

                new TestExcludeCase { ExcludeList = null, IncludeList = null, IncludeBoolean = true, ExpectInclude_200 = true, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = null, IncludeList = null, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = false },

                new TestExcludeCase { ExcludeList = new HttpStatusCode[]{ }, IncludeList = null, IncludeBoolean = true, ExpectInclude_200 = true, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = new HttpStatusCode[]{ }, IncludeList = null, IncludeBoolean = false, ExpectInclude_200 = true, ExpectInclude_400 = true },

                new TestExcludeCase { ExcludeList = null, IncludeList = new HttpStatusCode[]{ }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = false },
                new TestExcludeCase { ExcludeList = null, IncludeList = new HttpStatusCode[]{ }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = false },

                new TestExcludeCase { ExcludeList = new HttpStatusCode[]{ }, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = true },
                new TestExcludeCase { ExcludeList = new HttpStatusCode[]{ }, IncludeList = new [] { HttpStatusCode.BadRequest }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = true },

                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new HttpStatusCode[]{ }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = false },
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.OK }, IncludeList = new HttpStatusCode[]{ }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = false },

                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.Continue }, IncludeList = new HttpStatusCode[]{ HttpStatusCode.Forbidden }, IncludeBoolean = true, ExpectInclude_200 = false, ExpectInclude_400 = false },
                new TestExcludeCase { ExcludeList = new [] { HttpStatusCode.Continue }, IncludeList = new HttpStatusCode[]{ HttpStatusCode.Forbidden }, IncludeBoolean = false, ExpectInclude_200 = false, ExpectInclude_400 = false }

            };

            foreach (var testCase in testCases)
            {
                var attr = new AuditApiAttribute();
                attr.ExcludeResponseBodyFor = testCase.ExcludeList;
                attr.IncludeResponseBodyFor = testCase.IncludeList;
                attr.IncludeResponseBody = testCase.IncludeBoolean;

                var okIncluded = attr.ShouldIncludeResponseBody(HttpStatusCode.OK);
                var badIncluded = attr.ShouldIncludeResponseBody(HttpStatusCode.BadRequest);
                Assert.That(testCase.ExpectInclude_200, Is.EqualTo(okIncluded), $"Expect OK (200) included = {testCase.ExpectInclude_200}: {Configuration.JsonAdapter.Serialize(testCase)}");
                Assert.That(testCase.ExpectInclude_400, Is.EqualTo(badIncluded), $"Expect BadRequest (400) included = {testCase.ExpectInclude_400}: {Configuration.JsonAdapter.Serialize(testCase)}");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_AuditApiActionFilter_InsertOnEnd(bool injectDataProvider)
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
            request.SetupGet(r => r.QueryString).Returns(new QueryString(""));
            request.SetupGet(r => r.PathBase).Returns(new PathString(""));
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues> { { "content-type", "application/json" } }));
            request.Setup(c => c.ContentLength).Returns(123);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            httpResponse.Setup(c => c.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues> { {"header-one", "1" }, { "header-two", "2" } }));

            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);

            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));

            Mock<IServiceProvider> svcProvider = null;
            if (injectDataProvider)
            {
                Audit.Core.Configuration.DataProvider = null;
                svcProvider = new Mock<IServiceProvider>();
                svcProvider.Setup(s => s.GetService(It.IsAny<Type>()))
                    .Returns((Type t) => t == typeof(AuditDataProvider) ? dataProvider.Object : null);

                httpContext.SetupGet(c => c.RequestServices).Returns(() => svcProvider.Object);
            }
            else
            {
                Audit.Core.Configuration.DataProvider = dataProvider.Object;
            }

            var ci = new Mock<ConnectionInfo>();
            ci.SetupGet(_ => _.RemoteIpAddress).Returns(() => null);
            httpContext.SetupGet(c => c.Connection).Returns(() => ci.Object);

            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "get",
                    ControllerName = "values",
                    Parameters = new List<ParameterDescriptor>(),
                    MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First()
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                IncludeRequestBody = true,
                IncludeResponseHeaders = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            if (injectDataProvider)
            {
                svcProvider.Verify(p => p.GetService(It.IsAny<Type>()), Times.AtLeastOnce);
            }
            Assert.That(action.ActionExecutingContext, Is.Not.Null);
            Assert.That((action.ActionExecutingContext.ActionDescriptor as ControllerActionDescriptor).ActionName, Is.EqualTo((actionContext.ActionDescriptor as ControllerActionDescriptor).ActionName));
            Assert.That((action.ActionExecutingContext.ActionDescriptor as ControllerActionDescriptor).ControllerName, Is.EqualTo((actionContext.ActionDescriptor as ControllerActionDescriptor).ControllerName));
            Assert.That(action.RequestUrl, Is.EqualTo("http://200.10.10.20:1010/api/values"));
            Assert.That(action.Headers["content-type"], Is.EqualTo("application/json"));
            Assert.That(action.ControllerName, Is.EqualTo("values"));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.ResponseBody.Value, Is.EqualTo("this is the result"));
            Assert.That(action.ResponseHeaders.Count, Is.EqualTo(2));
            Assert.That(action.ResponseHeaders["header-one"], Is.EqualTo("1"));
            Assert.That(action.ResponseHeaders["header-two"], Is.EqualTo("2"));
            Assert.That(action.RequestBody.Length, Is.EqualTo(123));
            Assert.That(action.RequestBody.Type, Is.EqualTo("application/json"));
        }

        [Test]
        public async Task Test_AuditApiActionFilter_InheritedResultType()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
            request.SetupGet(r => r.QueryString).Returns(new QueryString(""));
            request.SetupGet(r => r.PathBase).Returns(new PathString(""));
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues> { { "content-type", "application/json" } }));
            request.Setup(c => c.ContentLength).Returns(123);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "get",
                    ControllerName = "values",
                    Parameters = new List<ParameterDescriptor>(),
                    MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First()
                }
            };
            var args = new Dictionary<string, object>();
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            Audit.Core.Configuration.DataProvider = new DynamicDataProvider();
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var filter = new AuditApiAttribute()
            {
                IncludeResponseBody = true
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new OkObjectResult("this is the result");

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;

            //Assert
            Assert.That(action.ResponseBody.Value, Is.EqualTo("this is the result"));
            Assert.That((action.GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName, Is.EqualTo((actionContext.ActionDescriptor as ControllerActionDescriptor).ActionName));
            Assert.That((action.GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ControllerName, Is.EqualTo((actionContext.ActionDescriptor as ControllerActionDescriptor).ControllerName));
        }

        [Test]
        [NonParallelizable]
        public async Task Test_AuditApiActionFilter_Manual()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
            request.SetupGet(r => r.QueryString).Returns(new QueryString(""));
            request.SetupGet(r => r.PathBase).Returns(new PathString(""));
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>{ { "content-type", "application/json" } }));
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "get",
                    ControllerName = "values",
                    Parameters = new List<ParameterDescriptor>(),
                    MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First()
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));

            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(action.RequestUrl, Is.EqualTo("http://200.10.10.20:1010/api/values"));
            Assert.That(action.Headers["content-type"], Is.EqualTo("application/json"));
            Assert.That(action.ControllerName, Is.EqualTo("values"));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.ResponseBody.Value, Is.EqualTo("this is the result"));
        }

        [Test]
        public async Task Test_AuditApiActionFilter_InsertOnStartReplaceOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
            request.SetupGet(r => r.QueryString).Returns(new QueryString(""));
            request.SetupGet(r => r.PathBase).Returns(new PathString(""));
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues> { { "content-type", "application/json" } }));
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "get",
                    ControllerName = "values",
                    Parameters = new List<ParameterDescriptor>(),
                    MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First()
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditTarget() { Type = "TEST_REFERENCE_TYPE" } }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;

            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            
            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;

            //Assert
            Assert.That((action.ActionParameters["x"] as AuditTarget).Type, Is.EqualTo("TEST_REFERENCE_TYPE"));
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(action.RequestUrl, Is.EqualTo("http://200.10.10.20:1010/api/values"));
            Assert.That(action.Headers["content-type"], Is.EqualTo("application/json"));
            Assert.That(action.ControllerName, Is.EqualTo("values"));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.ResponseBody.Value, Is.EqualTo("this is the result"));
        }
    }
}
#endif
