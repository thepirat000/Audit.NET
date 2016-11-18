#if NETCOREAPP1_0
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.Controllers;
using Audit.Core;
using System;

namespace Audit.WebApi.UnitTest
{
    public class ActionFilterUnitTest
    {
        [Fact]
        public void Test_AuditApiActionFilter_InsertOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
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
                    ControllerName = "values"
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.Equal(action, actionFromController);
            Assert.Equal(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.Equal("application/json", action.Headers["content-type"]);
            Assert.Equal("values", action.ControllerName);
            Assert.Equal("value1", action.ActionParameters["test1"]);
            Assert.Equal("this is the result", action.ResponseBody);
        }

        [Fact]
        public void Test_AuditApiActionFilter_Manual()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
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
                    ControllerName = "values"
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
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
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.Equal(action, actionFromController);
            Assert.Equal(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.Equal("application/json", action.Headers["content-type"]);
            Assert.Equal("values", action.ControllerName);
            Assert.Equal("value1", action.ActionParameters["test1"]);
            Assert.Equal("this is the result", action.ResponseBody);
        }

        [Fact]
        public void Test_AuditApiActionFilter_InsertOnStartReplaceOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.ContentType).Returns("application/json");
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/api/values");
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
                    ControllerName = "values"
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;

            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal(action, actionFromController);
            Assert.Equal(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal("http://200.10.10.20:1010/api/values", action.RequestUrl);
            Assert.Equal("application/json", action.Headers["content-type"]);
            Assert.Equal("values", action.ControllerName);
            Assert.Equal("value1", action.ActionParameters["test1"]);
            Assert.Equal("this is the result", action.ResponseBody);
        }
    }
}
#endif
