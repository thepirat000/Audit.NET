﻿#if ASP_NET
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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Specialized;

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

            foreach(var testCase in testCases)
            {
                var attr = new AuditApiAttribute();
                attr.ExcludeResponseBodyFor = testCase.ExcludeList;
                attr.IncludeResponseBodyFor = testCase.IncludeList;
                attr.IncludeResponseBody = testCase.IncludeBoolean;

                var okIncluded = attr.ShouldIncludeResponseBody(HttpStatusCode.OK);
                var badIncluded = attr.ShouldIncludeResponseBody(HttpStatusCode.BadRequest);
                Assert.That(okIncluded, Is.EqualTo(testCase.ExpectInclude_200), $"Expect OK (200) included = {testCase.ExpectInclude_200}: {Core.Configuration.JsonAdapter.Serialize(testCase)}");
                Assert.That(badIncluded, Is.EqualTo(testCase.ExpectInclude_400), $"Expect BadRequest (400) included = {testCase.ExpectInclude_400}: {Core.Configuration.JsonAdapter.Serialize(testCase)}");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_AuditApiActionFilter_InsertOnEnd(bool injectDataProvider)
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();

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
            httpResponse.Setup(c => c.Headers).Returns(new NameValueCollection()
                { { "header-one", "1" }, { "header-two", "2" } });
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
            var actionDescriptor = new CandidateHttpActionDescriptor_Test(new ReflectedHttpActionDescriptor()
            {
                MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First(),
                ActionBinding = new HttpActionBinding()
            });

            var args = new Dictionary<string, object>()
            {
                { "test1", "value1" }
            };

            var dataProvider = new Mock<IAuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            
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

            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor,
                ControllerContext = controllerContext,

            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);

            if (injectDataProvider)
            {
                Audit.Core.Configuration.DataProvider = null;
                httpContext.Setup(h => h.GetService(typeof(IAuditDataProvider)))
                    .Returns(dataProvider.Object);
            }
            else
            {
                Audit.Core.Configuration.DataProvider = dataProvider.Object;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("header-one", "1");
            response.Headers.Add("header-two", "2");
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null) { Response = response };
            using var ct = new CancellationTokenSource();
            await filter.OnActionExecutingAsync(actionExecutingContext, ct.Token);
            var scopeFromController = AuditApiAdapter.GetCurrentScope(controllerContext.Request, null);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();
            await filter.OnActionExecutedAsync(actionExecutedContext, ct.Token);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(action.Headers["test-header"], Is.EqualTo("header-value"));
            Assert.That(action.ActionName, Is.EqualTo(typeof(ActionFilterUnitTest).GetMethods().First().Name));
            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.AreEqual(123, ((dynamic)action.RequestBody).Length);
            Assert.AreEqual("application/json", ((dynamic)action.RequestBody).Type);
            Assert.That(action.ResponseHeaders.Count, Is.EqualTo(2));
            Assert.That(action.ResponseHeaders["header-one"], Is.EqualTo("1"));
            Assert.That(action.ResponseHeaders["header-two"], Is.EqualTo("2"));

            Assert.That(action.GetHttpActionContext().ActionDescriptor.ActionName, Is.EqualTo(actionContext.ActionDescriptor.ActionName));
        }


        [Test]
        public async Task Test_AuditApiActionFilter_Manual()
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
            var actionDescriptor = new ReflectedHttpActionDescriptor();
            actionDescriptor.MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First();
            actionDescriptor.ActionBinding = new HttpActionBinding();

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };

            var dataProvider = new Mock<IAuditDataProvider>();
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
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor,
                ControllerContext = controllerContext,

            };

            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null) { Response = new HttpResponseMessage(HttpStatusCode.OK) };
            using var ct = new CancellationTokenSource();
            await filter.OnActionExecutingAsync(actionExecutingContext, ct.Token);
            var scopeFromController = AuditApiAdapter.GetCurrentScope(controllerContext.Request, null);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();
            await filter.OnActionExecutedAsync(actionExecutedContext, ct.Token);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.Headers["test-header"], Is.EqualTo("header-value"));
            Assert.That(action.ActionName, Is.EqualTo(actionDescriptor.MethodInfo.Name));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
            Assert.That(action.HttpActionContext.ActionDescriptor.ActionName, Is.EqualTo(actionContext.ActionDescriptor.ActionName));
        }

        [Test]
        public async Task Test_AuditApiActionFilter_InsertOnStartReplaceOnEnd()
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
            var actionDescriptor = new ReflectedHttpActionDescriptor();
            actionDescriptor.MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First();
            actionDescriptor.ActionBinding = new HttpActionBinding();

            var arg = new AuditEvent() { EventType = "TEST_REFERENCE_TYPE" };

            var dataProvider = new Mock<IAuditDataProvider>();
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
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor,
                ControllerContext = controllerContext,

            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.ActionArguments.Add("x", arg);
            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null) { Response = new HttpResponseMessage(HttpStatusCode.OK) };
            using var ct = new CancellationTokenSource();
            await filter.OnActionExecutingAsync(actionExecutingContext, ct.Token);
            var scopeFromController = AuditApiAdapter.GetCurrentScope(controllerContext.Request, null);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();
            await filter.OnActionExecutedAsync(actionExecutedContext, ct.Token);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            var evtn = (action.ActionParameters["x"] as AuditEvent).EventType;
            Assert.That(evtn, Is.EqualTo("TEST_REFERENCE_TYPE"));
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.Headers["test-header"], Is.EqualTo("header-value"));
            Assert.That(action.ActionName, Is.EqualTo(actionDescriptor.MethodInfo.Name));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
        }


        [Test]
        public async Task Test_AuditApiActionFilter_CustomContextWrapper()
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
            var actionDescriptor = new ReflectedHttpActionDescriptor();
            actionDescriptor.MethodInfo = typeof(ActionFilterUnitTest).GetMethods().First();
            actionDescriptor.ActionBinding = new HttpActionBinding();

            var arg = new AuditEvent() { EventType = "TEST_REFERENCE_TYPE" };

            var dataProvider = new Mock<IAuditDataProvider>();
            dataProvider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Task.FromResult(Guid.NewGuid()));
            Audit.Core.Configuration.DataProvider = dataProvider.Object;
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            var filter = new AuditApiAttribute()
            {
                IncludeHeaders = true,
                IncludeModelState = true,
                IncludeResponseBody = true,
                EventTypeName = "TestEvent",
                SerializeActionParameters = true,
                ContextWrapperType = typeof(CustomContextWrapper)
            };
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor,
                ControllerContext = controllerContext,
                
            };
            var actionExecutingContext = new HttpActionContext(controllerContext, actionDescriptor);
            actionExecutingContext.ActionArguments.Add("test1", "value1");
            actionExecutingContext.ActionArguments.Add("x", arg);
            var self = new TestClass() { Id = 1 };
            actionExecutingContext.ActionArguments.Add("SelfReferencing", self);

            actionExecutingContext.Request.Properties.Add("MS_HttpContext", httpContext.Object);
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null) { Response = new HttpResponseMessage(HttpStatusCode.OK) };
            using var ct = new CancellationTokenSource();
            await filter.OnActionExecutingAsync(actionExecutingContext, ct.Token);
            var scopeFromController = AuditApiAdapter.GetCurrentScope(controllerContext.Request, null);
            var actionFromController = scopeFromController.Event.GetWebApiAuditAction();
            await filter.OnActionExecutedAsync(actionExecutedContext, ct.Token);

            var action = itemsDict["__private_AuditApiAction__"] as AuditApiAction;
            var scope = itemsDict["__private_AuditApiScope__"] as AuditScope;

            //Assert
            var evtn = (action.ActionParameters["x"] as AuditEvent).EventType;
            Assert.That(evtn, Is.EqualTo("TEST_REFERENCE_TYPE"));
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            dataProvider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            dataProvider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(actionFromController, Is.EqualTo(action));
            Assert.That(scopeFromController, Is.EqualTo(scope));
            Assert.That(action.Headers["test-header"], Is.EqualTo("header-value"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.ActionName));
            Assert.That(action.ActionParameters["test1"], Is.EqualTo("value1"));
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

    public class CustomContextWrapper : ContextWrapper
    {
        private readonly HttpRequestMessage _request;
        public CustomContextWrapper(HttpRequestMessage request) : base(request)
        {
            _request = request;
        }
        public override HttpContextBase GetHttpContext()
        {
            _request.Properties.TryGetValue("MS_HttpContext", out object obj);
            return obj as HttpContextBase;
        }
    }
}
#endif