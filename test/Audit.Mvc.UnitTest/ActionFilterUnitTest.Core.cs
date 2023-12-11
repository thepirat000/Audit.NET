#if ASP_CORE
using Audit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Mvc.UnitTest
{
    [TestFixture]
    public class ActionFilterUnitTest
    {
        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_InsertOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1")
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
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;
            
            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(action, Is.EqualTo(actionFromController));
            Assert.That(scope, Is.EqualTo(scopeFromController));
            Assert.That("http://200.10.10.20:1010/home/index", Is.EqualTo(action.RequestUrl));
            Assert.That("home", Is.EqualTo(action.ControllerName));
            Assert.That("value1", Is.EqualTo(action.ActionParameters["test1"]));
            Assert.That(200, Is.EqualTo(action.ResponseStatusCode));
        }

        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_InsertOnStartReplaceOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1")
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditTarget(){ Type="TEST_REFERENCE_TYPE" } }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true
            };
            
            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            Assert.That("TEST_REFERENCE_TYPE", Is.EqualTo((action.ActionParameters["x"] as AuditTarget).Type));
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(((AuditAction)scopeFromController.Event.GetMvcAuditAction()).ResponseStatus, Is.Not.Null);

            Assert.That(action, Is.EqualTo(actionFromController));
            Assert.That(scope, Is.EqualTo(scopeFromController));
            Assert.That("http://200.10.10.20:1010/home/index", Is.EqualTo(action.RequestUrl));
            Assert.That("home", Is.EqualTo(action.ControllerName));
            Assert.That("value1", Is.EqualTo(action.ActionParameters["test1"]));
            Assert.That(200, Is.EqualTo(action.ResponseStatusCode));
        }

        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_IgnoreMethod()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1_Ignored")
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditAttribute(){ EventTypeName="TEST_REFERENCE_TYPE" } }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            Assert.That(itemsDict.ContainsKey("__private_AuditAction__"), Is.False);
            Assert.That(itemsDict.ContainsKey("__private_AuditScope__"), Is.False);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_IgnoreParam()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1_IgnoredParam")
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditTarget(){ Type="TEST_REFERENCE_TYPE" } }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            //Assert
            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(action.ActionParameters.Count, Is.EqualTo(1));
            Assert.That((action.ActionParameters["x"] as AuditTarget).Type, Is.EqualTo("TEST_REFERENCE_TYPE"));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }


        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_Manual()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1")
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
            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };


            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(((AuditAction)scopeFromController.Event.GetMvcAuditAction()).ResponseStatus, Is.Not.Null);

            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.RequestUrl, Is.EqualTo("http://200.10.10.20:1010/home/index"));
            Assert.That(action.ControllerName, Is.EqualTo("home"));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Test_MVC_AuditActionFilter_Core_IncludeResquestResponse()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
            request.SetupGet(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(@"{ ""test"": 123 }")));
            request.SetupGet(r => r.ContentLength).Returns(@"{ ""test"": 123 }".Length);
            request.SetupGet(r => r.ContentType).Returns(@"json object");
            
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
                    ActionName = "index",
                    ControllerName = "home",
                    ControllerTypeInfo = typeof(MockMethodInfo).GetTypeInfo(),
                    MethodInfo = typeof(MockMethodInfo).GetMethod("Method1")
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
            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent",
                IncludeRequestBody = true,
                IncludeResponseBody = true
            };


            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");

            var resultExecuting = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);
            var resultExecute = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new RedirectResult("url"), controller.Object);

            await filter.OnActionExecutionAsync(actionExecutingContext, async () => await Task.FromResult(actionExecutedContext));
            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();
            await filter.OnResultExecutionAsync(resultExecuting, () => Task.FromResult<ResultExecutedContext>(resultExecute));

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(((AuditAction)scopeFromController.Event.GetMvcAuditAction()).ResponseStatus, Is.Not.Null);

            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.RequestUrl, Is.EqualTo("http://200.10.10.20:1010/home/index"));
            Assert.That(action.ControllerName, Is.EqualTo("home"));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(action.RequestBody.Type, Is.EqualTo(@"json object"));
            Assert.That(action.RequestBody.Value, Is.EqualTo(@"{ ""test"": 123 }"));
            Assert.That(action.RequestBody.Length, Is.EqualTo(@"{ ""test"": 123 }".Length));
            Assert.That(action.ResponseBody.Type, Is.EqualTo("RedirectResult"));
            Assert.That(action.ResponseBody.Value, Is.EqualTo("url"));
        }
    }
}
#endif