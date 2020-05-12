#if NETCOREAPP3_1 || NETCOREAPP2_0 || NETCOREAPP1_0 || NET451
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
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Reflection;
using System.Linq;

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
                Assert.AreEqual(testCase.ExpectInclude_200, okIncluded, $"Expect OK (200) included = {testCase.ExpectInclude_200}: {JsonConvert.SerializeObject(testCase)}");
                Assert.AreEqual(testCase.ExpectInclude_400, badIncluded, $"Expect BadRequest (400) included = {testCase.ExpectInclude_400}: {JsonConvert.SerializeObject(testCase)}");
            }
        }

        [Test]
        public async Task Test_AuditApiActionFilter_InsertOnEnd()
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
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
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
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.IsNotNull(action.ActionExecutingContext);
            Assert.AreEqual((actionContext.ActionDescriptor as ControllerActionDescriptor).ActionName, (action.ActionExecutingContext.ActionDescriptor as ControllerActionDescriptor).ActionName);
            Assert.AreEqual((actionContext.ActionDescriptor as ControllerActionDescriptor).ControllerName, (action.ActionExecutingContext.ActionDescriptor as ControllerActionDescriptor).ControllerName);
            Assert.AreEqual("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.AreEqual("application/json", action.Headers["content-type"]);
            Assert.AreEqual("values", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual("this is the result", action.ResponseBody.Value);
            Assert.AreEqual(2, action.ResponseHeaders.Count);
            Assert.AreEqual("1", action.ResponseHeaders["header-one"]);
            Assert.AreEqual("2", action.ResponseHeaders["header-two"]);
            Assert.AreEqual(123, action.RequestBody.Length);
            Assert.AreEqual("application/json", action.RequestBody.Type);
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
            Assert.AreEqual("this is the result", action.ResponseBody.Value);

            Assert.AreEqual((actionContext.ActionDescriptor as ControllerActionDescriptor).ActionName, (action.GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName);
            Assert.AreEqual((actionContext.ActionDescriptor as ControllerActionDescriptor).ControllerName, (action.GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ControllerName);
        }

        [Test]
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
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
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

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.AreEqual("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.AreEqual("application/json", action.Headers["content-type"]);
            Assert.AreEqual("values", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual("this is the result", action.ResponseBody.Value);
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
                {"x", new AuditApiAttribute(){ EventTypeName="TEST_REFERENCE_TYPE" } }

            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
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
            Assert.AreEqual("TEST_REFERENCE_TYPE", (action.ActionParameters["x"] as AuditApiAttribute).EventTypeName);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            Assert.AreEqual("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.AreEqual("application/json", action.Headers["content-type"]);
            Assert.AreEqual("values", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual("this is the result", action.ResponseBody.Value);
        }
    }
}
#endif
