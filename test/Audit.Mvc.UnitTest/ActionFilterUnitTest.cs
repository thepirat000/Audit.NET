#if NET45 || NET461
using Audit.Core;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Audit.Mvc.UnitTest
{
    [TestFixture]
    public class ActionFilterUnitTest
    {
        [Test]
        public void Test_MVC_AuditActionFilter_InsertOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();
            var nvc = new NameValueCollection();
            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");
            request.Setup(c => c.Unvalidated.Headers).Returns(() => nvc);
            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };
            controllerContext.HttpContext.Request.Unvalidated.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditAttribute(){ EventTypeName="TEST_REFERENCE_TYPE" } }
            };
            
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true
            };
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, args );
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();

            (args["x"] as AuditAttribute).EventTypeName = "CHANGED!";

            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            var evtn = (action.ActionParameters["x"] as AuditAttribute).EventTypeName;
            Assert.AreEqual("TEST_REFERENCE_TYPE", evtn);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());

            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never());
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
        }

        [Test]
        public void Test_MVC_AuditActionFilter_Manual()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();
            var nvc = new NameValueCollection();
            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");
            request.Setup(c => c.Unvalidated.Headers).Returns(() => nvc);

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };
            controllerContext.HttpContext.Request.Unvalidated.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };

            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, new Dictionary<string, object> { { "test1", "value1" } });
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();

            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never());
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
        }

        [Test]
        public void Test_MVC_AuditActionFilter_InsertOnStartReplaceOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();
            var nvc = new NameValueCollection();
            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");
            request.Setup(c => c.Unvalidated.Headers).Returns(() => nvc);

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };
            controllerContext.HttpContext.Request.Unvalidated.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };

            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, new Dictionary<string, object> { { "test1", "value1" } });
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditAttribute.GetCurrentScope(httpContext.Object);
            var actionFromController = scopeFromController.Event.GetMvcAuditAction();

            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once());
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
        }
    }
}
#endif