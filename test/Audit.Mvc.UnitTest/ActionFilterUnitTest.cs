#if ASP_NET
using Audit.Core;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
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
            request.Setup(c => c.Headers).Returns(() => nvc);
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
            controllerContext.HttpContext.Request.Headers.Add("test-header", "header-value");

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var param2Descriptor = new Mock<ParameterDescriptor>();
            param2Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param2Descriptor.Setup(c => c.ParameterName).Returns("x");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object, param2Descriptor.Object });

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditEvent(){ EventType="TEST_REFERENCE_TYPE" } }
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

            (args["x"] as AuditEvent).EventType = "CHANGED!";

            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            var evtn = (action.ActionParameters["x"] as AuditEvent).EventType;
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
            request.Setup(c => c.Headers).Returns(() => nvc);

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
            controllerContext.HttpContext.Request.Headers.Add("test-header", "header-value");

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var param2Descriptor = new Mock<ParameterDescriptor>();
            param2Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param2Descriptor.Setup(c => c.ParameterName).Returns("x");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object, param2Descriptor.Object });


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
            request.Setup(c => c.Headers).Returns(() => nvc);

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
            controllerContext.HttpContext.Request.Headers.Add("test-header", "header-value");

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var param2Descriptor = new Mock<ParameterDescriptor>();
            param2Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param2Descriptor.Setup(c => c.ParameterName).Returns("x");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object, param2Descriptor.Object });


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

        [Test]
        public void Test_MVC_AuditActionFilter_IncludeResquestResponse()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();
            var nvc = new NameValueCollection();
            
            request.Setup(c => c.ContentType).Returns("application/json");
            request.Setup(c => c.Unvalidated.Headers).Returns(() => nvc);
            request.Setup(c => c.Headers).Returns(() => nvc);

            request.SetupGet(r => r.InputStream).Returns(new MemoryStream(Encoding.UTF8.GetBytes(@"{ ""test"": 123 }")));
            request.SetupGet(r => r.ContentLength).Returns(@"{ ""test"": 123 }".Length);
            request.SetupGet(r => r.ContentType).Returns(@"json object");


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
            controllerContext.HttpContext.Request.Headers.Add("test-header", "header-value");

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var param2Descriptor = new Mock<ParameterDescriptor>();
            param2Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param2Descriptor.Setup(c => c.ParameterName).Returns("x");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object, param2Descriptor.Object });


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
                EventTypeName = "TestEvent",
                IncludeRequestBody = true,
                IncludeResponseBody = true
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

            Assert.AreEqual(@"json object", action.RequestBody.Type);
            Assert.AreEqual(@"{ ""test"": 123 }", action.RequestBody.Value);
            Assert.AreEqual(@"{ ""test"": 123 }".Length, action.RequestBody.Length);
            Assert.AreEqual("RedirectResult", action.ResponseBody.Type);
            Assert.AreEqual("url", action.ResponseBody.Value);

        }

        [Test]
        public void Test_MVC_AuditActionFilter_IgnoreMethod()
        {
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

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { new AuditIgnoreAttribute() }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object });

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
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, args);
            filter.OnActionExecuting(actionExecutingContext);
            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            // Assert
            Assert.IsFalse(itemsDict.ContainsKey("__private_AuditAction__"));
            Assert.IsFalse(itemsDict.ContainsKey("__private_AuditScope__"));
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never());
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never());

        }

        [Test]
        public void Test_MVC_AuditActionFilter_IgnoreParam()
        {
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

            var ctrlDescriptor = new Mock<ControllerDescriptor>();
            ctrlDescriptor.Setup(c => c.ControllerType).Returns(this.GetType()); // controller type (to check controller-level ignore)

            var param1Descriptor = new Mock<ParameterDescriptor>();
            param1Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { new AuditIgnoreAttribute() }); // custom attribs for param
            param1Descriptor.Setup(c => c.ParameterName).Returns("test1");

            var param2Descriptor = new Mock<ParameterDescriptor>();
            param2Descriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for param
            param2Descriptor.Setup(c => c.ParameterName).Returns("x");

            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");
            actionDescriptor.Setup(c => c.ControllerDescriptor).Returns(ctrlDescriptor.Object);
            actionDescriptor.Setup(c => c.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>())).Returns(new object[] { }); // custom attribs for method
            actionDescriptor.Setup(c => c.GetParameters()).Returns(new ParameterDescriptor[] { param1Descriptor.Object, param2Descriptor.Object });

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" },
                {"x", new AuditEvent() { EventType = "TEST" } }
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
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, args);
            filter.OnActionExecuting(actionExecutingContext);
            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var resultExecute = new ResultExecutedContext(controllerContext, new RedirectResult("url"), false, null);
            filter.OnResultExecuted(resultExecute);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once());
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never());
            Assert.AreEqual(1, action.ActionParameters.Count);
            Assert.AreEqual("TEST", (action.ActionParameters["x"] as AuditEvent).EventType);
        }
    }
}
#endif