#if ASP_CORE
using System.Collections.Generic;
using Audit.WebApi.ConfigurationApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

using NUnit.Framework;

namespace Audit.WebApi.UnitTest
{
    [TestFixture]
    public class AuditApiGlobalConfiguratorTests
    {
        private AuditApiGlobalConfigurator _configurator;

        [SetUp]
        public void SetUp()
        {
            _configurator = new AuditApiGlobalConfigurator();
        }

        [Test]
        public void WithEventType_String_SetsEventTypeNameBuilder()
        {
            var result = _configurator.WithEventType("TestType");
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._eventTypeNameBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._eventTypeNameBuilder(context), Is.EqualTo("TestType"));
        }

        [Test]
        public void WithEventType_Func_SetsEventTypeNameBuilder()
        {
            string expected = "DynamicType";
            var result = _configurator.WithEventType(ctx => expected);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._eventTypeNameBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._eventTypeNameBuilder(context), Is.EqualTo(expected));
        }

        [Test]
        public void IncludeHeaders_Bool_SetsIncludeRequestHeadersBuilder()
        {
            var result = _configurator.IncludeHeaders(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeRequestHeadersBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._includeRequestHeadersBuilder(context), Is.True);

            _configurator.IncludeHeaders(false);
            Assert.That(_configurator._includeRequestHeadersBuilder(context), Is.False);
        }

        [Test]
        public void IncludeHeaders_Func_SetsIncludeRequestHeadersBuilder()
        {
            var result = _configurator.IncludeHeaders(ctx => ctx != null);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeRequestHeadersBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._includeRequestHeadersBuilder(context), Is.True);
            Assert.That(_configurator._includeRequestHeadersBuilder(null), Is.False);
        }

        [Test]
        public void IncludeResponseHeaders_Bool_SetsIncludeResponseHeadersBuilder()
        {
            var result = _configurator.IncludeResponseHeaders(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeResponseHeadersBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeResponseHeadersBuilder(context), Is.True);

            _configurator.IncludeResponseHeaders(false);
            Assert.That(_configurator._includeResponseHeadersBuilder(context), Is.False);
        }

        [Test]
        public void IncludeResponseHeaders_Func_SetsIncludeResponseHeadersBuilder()
        {
            var result = _configurator.IncludeResponseHeaders(ctx => ctx != null);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeResponseHeadersBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeResponseHeadersBuilder(context), Is.True);
            Assert.That(_configurator._includeResponseHeadersBuilder(null), Is.False);
        }

        [Test]
        public void IncludeModelState_Bool_SetsIncludeModelStateBuilder()
        {
            var result = _configurator.IncludeModelState(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeModelStateBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeModelStateBuilder(context), Is.True);

            _configurator.IncludeModelState(false);
            Assert.That(_configurator._includeModelStateBuilder(context), Is.False);
        }

        [Test]
        public void IncludeModelState_Func_SetsIncludeModelStateBuilder()
        {
            var result = _configurator.IncludeModelState(ctx => ctx != null);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeModelStateBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeModelStateBuilder(context), Is.True);
            Assert.That(_configurator._includeModelStateBuilder(null), Is.False);
        }

        [Test]
        public void IncludeRequestBody_Bool_SetsIncludeRequestBodyBuilder()
        {
            var result = _configurator.IncludeRequestBody(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeRequestBodyBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._includeRequestBodyBuilder(context), Is.True);

            _configurator.IncludeRequestBody(false);
            Assert.That(_configurator._includeRequestBodyBuilder(context), Is.False);
        }

        [Test]
        public void IncludeRequestBody_Func_SetsIncludeRequestBodyBuilder()
        {
            var result = _configurator.IncludeRequestBody(ctx => ctx != null);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeRequestBodyBuilder, Is.Not.Null);

            var context = GetActionExecutingContext();
            Assert.That(_configurator._includeRequestBodyBuilder(context), Is.True);
            Assert.That(_configurator._includeRequestBodyBuilder(null), Is.False);
        }

        [Test]
        public void IncludeResponseBody_Bool_SetsIncludeResponseBodyBuilder()
        {
            var result = _configurator.IncludeResponseBody(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeResponseBodyBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeResponseBodyBuilder(context), Is.True);

            _configurator.IncludeResponseBody(false);
            Assert.That(_configurator._includeResponseBodyBuilder(context), Is.False);
        }

        [Test]
        public void IncludeResponseBody_Func_SetsIncludeResponseBodyBuilder()
        {
            var result = _configurator.IncludeResponseBody(ctx => ctx != null);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._includeResponseBodyBuilder, Is.Not.Null);

            var context = GetActionExecutedContext();
            Assert.That(_configurator._includeResponseBodyBuilder(context), Is.True);
            Assert.That(_configurator._includeResponseBodyBuilder(null), Is.False);
        }

        [Test]
        public void SerializeActionParameters_SetsSerializeActionParameters()
        {
            var result = _configurator.SerializeActionParameters(true);
            Assert.That(result, Is.SameAs(_configurator));
            Assert.That(_configurator._serializeActionParameters, Is.True);

            _configurator.SerializeActionParameters(false);
            Assert.That(_configurator._serializeActionParameters, Is.False);
        }

        private ActionExecutingContext GetActionExecutingContext()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => new Dictionary<object, object>());
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
            var controller = new Mock<Controller>();

            var actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller.Object);

            return actionExecutingContext;
        }

        private ActionExecutedContext GetActionExecutedContext()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => new Dictionary<object, object>());
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
            var controller = new Mock<Controller>();

            var actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller.Object);

            return actionExecutedContext;
        }
    }
}
#endif