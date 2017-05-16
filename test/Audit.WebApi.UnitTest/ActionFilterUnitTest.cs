#if NET451
using Moq;
using System.Collections.Generic;
using System.Web;
using Audit.Core;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net;
using System.Net.Http;
using System;
using NUnit.Framework;
using System.IO;
using Newtonsoft.Json;

namespace Audit.WebApi.UnitTest
{
    public class ActionFilterUnitTest
    {
        [Test]
        public void Test_AuditApiActionFilter_InsertOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();

            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("{ Id: 'test' }");
            writer.Flush();
            stream.Position = 0;
            request.Setup(c => c.InputStream).Returns(stream);
            request.Setup(c => c.ContentLength).Returns(123);

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new HttpControllerContext()
            {
                ControllerDescriptor = new HttpControllerDescriptor()
                {
                    ControllerName = "values"
                },
                Request = new HttpRequestMessage()
            };
            controllerContext.Request.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<HttpActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            
            var dataProvider = new Mock<AuditDataProvider>();
            dataProvider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns(Guid.NewGuid());
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                IncludeRequestBody = true,
                EventTypeName = "TestEvent"
            };
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor.Object,
                ControllerContext = controllerContext,
                
            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor.Object);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            var self = new TestClass() { Id = 1 };
            actionExecutingContext.ActionArguments.Add("SelfReferencing", self);
            Console.WriteLine(JsonConvert.SerializeObject(self, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);
            
            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(controllerContext.Request);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null);
            actionExecutedContext.Response = new System.Net.Http.HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            };
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);

            Assert.AreEqual(123, ((dynamic)action.RequestBody).Length);
            Assert.AreEqual("application/json", ((dynamic)action.RequestBody).Type);
        }

        [Test]
        public void Test_AuditApiActionFilter_Manual()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();

            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new HttpControllerContext()
            {
                ControllerDescriptor = new HttpControllerDescriptor()
                {
                    ControllerName = "values"
                },
                Request = new HttpRequestMessage()
            };
            controllerContext.Request.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<HttpActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };

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
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor.Object,
                ControllerContext = controllerContext,

            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor.Object);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);

            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(controllerContext.Request);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null);
            actionExecutedContext.Response = new System.Net.Http.HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            };
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
        }

        [Test]
        public void Test_AuditApiActionFilter_InsertOnStartReplaceOnEnd()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();

            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new HttpControllerContext()
            {
                ControllerDescriptor = new HttpControllerDescriptor()
                {
                    ControllerName = "values"
                },
                Request = new HttpRequestMessage()
            };
            controllerContext.Request.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<HttpActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };

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
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor.Object,
                ControllerContext = controllerContext,

            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor.Object);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);

            filter.OnActionExecuting(actionExecutingContext);

            var scopeFromController = AuditApiAttribute.GetCurrentScope(controllerContext.Request);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();

            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null);
            actionExecutedContext.Response = new System.Net.Http.HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            };
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            Assert.AreEqual(action, actionFromController);
            Assert.AreEqual(scope, scopeFromController);
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.AreEqual("header-value", action.Headers["test-header"]);
            Assert.AreEqual("get", action.ActionName);
            Assert.AreEqual("value1", action.ActionParameters["test1"]);
        }
    }

    public class TestClass
    {
        public int Id { get; set; }
        public TestClass Self { get; set; }
        public TestClass()
        {
            Self = this;
        }
    }
}
#endif