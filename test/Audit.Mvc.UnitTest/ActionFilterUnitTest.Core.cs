#if NETCOREAPP1_0 || NETCOREAPP2_0 || NET451
using Audit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "index",
                    ControllerName = "home"
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
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            Assert.AreEqual("http://200.10.10.20:1010/home/index", action.RequestUrl);
            Assert.AreEqual("home", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual(200, action.ResponseStatusCode);
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
                    ControllerName = "home"
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
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
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
            Assert.AreEqual("TEST_REFERENCE_TYPE", (action.ActionParameters["x"] as AuditAttribute).EventTypeName);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            Assert.NotNull(((AuditAction)scopeFromController.Event.GetMvcAuditAction()).ResponseStatus);

            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            Assert.AreEqual("http://200.10.10.20:1010/home/index", action.RequestUrl);
            Assert.AreEqual("home", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual(200, action.ResponseStatusCode);
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
                    ControllerName = "home"
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
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.NotNull(((AuditAction)scopeFromController.Event.GetMvcAuditAction()).ResponseStatus);

            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            Assert.AreEqual("http://200.10.10.20:1010/home/index", action.RequestUrl);
            Assert.AreEqual("home", action.ControllerName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
            Assert.AreEqual(200, action.ResponseStatusCode);
        }
    }
}
#endif