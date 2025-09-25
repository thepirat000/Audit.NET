#if ASP_NET
using Audit.Core;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Audit.WebApi.UnitTest
{
    [TestFixture]
    public class AuditApiAdapterNetTests
    {
        private AuditApiAdapter _adapter;

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            Audit.Core.Configuration.Setup().UseNullProvider();

            _adapter = new AuditApiAdapter();

        }

        [Test]
        public void IsActionIgnored_ReturnsFalse_WhenNoAttributes()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));

            var result = _adapter.IsActionIgnored(actionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenControllerHasAuditIgnoreAttribute()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTestIgnored), typeof(ControllerTestIgnored).GetMethod("Get"));

            var result = _adapter.IsActionIgnored(actionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsActionIgnored_ReturnsTrue_WhenActionHasAuditIgnoreAttribute()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("GetIgnored"));
            
            var result = _adapter.IsActionIgnored(actionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsActionIgnored_ReturnsFalse_WhenActionContextIsNull()
        {
            var result = _adapter.IsActionIgnored(null);

            Assert.That(result, Is.False);
        }
        
        [Test]
        public async Task BeforeExecutingAsync_SetsAuditActionAndScope_WithHeadersAndRequestBody()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var contextWrapper = new Mock<IContextWrapper>();
            var httpContext = new Mock<HttpContextBase>();
            var scopeFactory = new Mock<IAuditScopeFactory>();
            var auditScope = new Mock<IAuditScope>();
            var dataProvider = new Mock<IAuditDataProvider>();

            // Setup context wrapper
            contextWrapper.Setup(x => x.GetClientIp()).Returns("127.0.0.1");
            contextWrapper.Setup(x => x.GetFormVariables()).Returns(new System.Collections.Generic.Dictionary<string, string> { { "key", "value" } });
            contextWrapper.Setup(x => x.GetHttpContext()).Returns(httpContext.Object);

            // Setup httpContext service resolution
            httpContext.Setup(x => x.GetService(typeof(IAuditScopeFactory))).Returns(scopeFactory.Object);
            httpContext.Setup(x => x.GetService(typeof(IAuditDataProvider))).Returns(dataProvider.Object);

            // Setup scope factory
            scopeFactory.Setup(x => x.CreateAsync(It.IsAny<AuditScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditScope.Object);

            // Setup contextWrapper.Set
            AuditApiAction capturedAuditAction = null;
            IAuditScope capturedAuditScope = null;
            contextWrapper.Setup(x => x.Set(AuditApiHelper.AuditApiActionKey, It.IsAny<AuditApiAction>()))
                .Callback<string, AuditApiAction>((k, v) => capturedAuditAction = v);
            contextWrapper.Setup(x => x.Set(AuditApiHelper.AuditApiScopeKey, It.IsAny<IAuditScope>()))
                .Callback<string, IAuditScope>((k, v) => capturedAuditScope = v);

            await _adapter.BeforeExecutingAsync(
                actionContext,
                contextWrapper.Object,
                includeHeaders: true,
                includeRequestBody: true,
                serializeParams: true,
                eventTypeName: "CustomType",
                CancellationToken.None);

            Assert.That(capturedAuditAction, Is.Not.Null);
            Assert.That(capturedAuditAction.UserName, Is.Null); // No principal set
            Assert.That(capturedAuditAction.IpAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(capturedAuditAction.FormVariables.ContainsKey("key"), Is.True);
            Assert.That(capturedAuditAction.Headers, Is.Not.Null);
            Assert.That(capturedAuditAction.ActionName, Is.EqualTo("Get"));
            Assert.That(capturedAuditAction.ControllerName, Is.EqualTo("values"));
            Assert.That(capturedAuditAction.ActionParameters, Is.Not.Null);
            Assert.That(capturedAuditAction.RequestBody, Is.Null.Or.TypeOf<BodyContent>()); // GetRequestBody returns null unless mocked
            Assert.That(capturedAuditAction.TraceId, Is.Not.Null);
            Assert.That(capturedAuditAction.HttpActionContext, Is.EqualTo(actionContext));
            Assert.That(capturedAuditScope, Is.Not.Null);
        }
        
        [Test]
        public async Task BeforeExecutingAsync_SetsAuditAction_WithoutHeadersOrRequestBody()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var contextWrapper = new Mock<IContextWrapper>();
            var httpContext = new Mock<HttpContextBase>();
            var scopeFactory = new Mock<IAuditScopeFactory>();
            var auditScope = new Mock<IAuditScope>();
            var dataProvider = new Mock<IAuditDataProvider>();

            contextWrapper.Setup(x => x.GetClientIp()).Returns("192.168.1.1");
            contextWrapper.Setup(x => x.GetFormVariables()).Returns(new System.Collections.Generic.Dictionary<string, string>());
            contextWrapper.Setup(x => x.GetHttpContext()).Returns(httpContext.Object);

            httpContext.Setup(x => x.GetService(typeof(IAuditScopeFactory))).Returns(scopeFactory.Object);
            httpContext.Setup(x => x.GetService(typeof(IAuditDataProvider))).Returns(dataProvider.Object);

            scopeFactory.Setup(x => x.CreateAsync(It.IsAny<AuditScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditScope.Object);

            AuditApiAction capturedAuditAction = null;
            IAuditScope capturedAuditScope = null;
            contextWrapper.Setup(x => x.Set(AuditApiHelper.AuditApiActionKey, It.IsAny<AuditApiAction>()))
                .Callback<string, AuditApiAction>((k, v) => capturedAuditAction = v);
            contextWrapper.Setup(x => x.Set(AuditApiHelper.AuditApiScopeKey, It.IsAny<IAuditScope>()))
                .Callback<string, IAuditScope>((k, v) => capturedAuditScope = v);

            await _adapter.BeforeExecutingAsync(
                actionContext,
                contextWrapper.Object,
                includeHeaders: false,
                includeRequestBody: false,
                serializeParams: false,
                eventTypeName: null,
                CancellationToken.None);

            Assert.That(capturedAuditAction, Is.Not.Null);
            Assert.That(capturedAuditAction.Headers, Is.Null);
            Assert.That(capturedAuditAction.RequestBody, Is.Null);
            Assert.That(capturedAuditAction.TraceId, Is.Not.Null);
            Assert.That(capturedAuditScope, Is.Not.Null);
        }

        [Test]
        public async Task AfterExecutedAsync_SetsAuditAction_WithResponseHeadersAndBody()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null)
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    ReasonPhrase = "OK",
                    Content = new StringContent("response body")
                }
            };

            var auditAction = new AuditApiAction();
            var auditEventWebApi = new AuditEventWebApi();
            var auditScopeMock = await AuditScope.CreateAsync(c => c.EventType("test").AuditEvent(auditEventWebApi));
            var contextWrapper = new Mock<IContextWrapper>();

            contextWrapper.Setup(x => x.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey)).Returns(auditAction);
            contextWrapper.Setup(x => x.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey)).Returns(auditScopeMock as AuditScope);

            await _adapter.AfterExecutedAsync(
                actionExecutedContext,
                contextWrapper.Object,
                includeModelState: true,
                includeResponseBody: true,
                includeResponseHeaders: true,
                CancellationToken.None);

            Assert.That(auditAction.ResponseStatus, Is.EqualTo("OK"));
            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(auditAction.ResponseBody, Is.Not.Null);
            Assert.That(auditAction.ResponseHeaders, Is.Not.Null);
            Assert.That(auditEventWebApi.Action, Is.EqualTo(auditAction));
        }

        [Test]
        public async Task AfterExecutedAsync_SetsAuditAction_WithObjectContent()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null)
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    ReasonPhrase = "OK",
                    Content = new ObjectContent(typeof(string), "{\"id\": 1}", new JsonMediaTypeFormatter()) 
                }
            };

            var auditAction = new AuditApiAction();
            var auditEventWebApi = new AuditEventWebApi();
            var auditScopeMock = await AuditScope.CreateAsync(c => c.EventType("test").AuditEvent(auditEventWebApi));
            var contextWrapper = new Mock<IContextWrapper>();

            contextWrapper.Setup(x => x.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey)).Returns(auditAction);
            contextWrapper.Setup(x => x.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey)).Returns(auditScopeMock as AuditScope);

            await _adapter.AfterExecutedAsync(
                actionExecutedContext,
                contextWrapper.Object,
                includeModelState: true,
                includeResponseBody: true,
                includeResponseHeaders: true,
                CancellationToken.None);

            Assert.That(auditAction.ResponseStatus, Is.EqualTo("OK"));
            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(auditAction.ResponseBody, Is.Not.Null);
            Assert.That(auditAction.ResponseHeaders, Is.Not.Null);
            Assert.That(auditEventWebApi.Action, Is.EqualTo(auditAction));
        }

        [Test]
        public async Task AfterExecutedAsync_SetsAuditAction_WithNoContent()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null)
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    ReasonPhrase = "OK",
                    Content = null
                }
            };

            var auditAction = new AuditApiAction();
            var auditEventWebApi = new AuditEventWebApi();
            var auditScopeMock = await AuditScope.CreateAsync(c => c.EventType("test").AuditEvent(auditEventWebApi));
            var contextWrapper = new Mock<IContextWrapper>();

            contextWrapper.Setup(x => x.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey)).Returns(auditAction);
            contextWrapper.Setup(x => x.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey)).Returns(auditScopeMock as AuditScope);

            await _adapter.AfterExecutedAsync(
                actionExecutedContext,
                contextWrapper.Object,
                includeModelState: true,
                includeResponseBody: true,
                includeResponseHeaders: true,
                CancellationToken.None);

            Assert.That(auditAction.ResponseStatus, Is.EqualTo("OK"));
            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(auditAction.ResponseBody, Is.Not.Null);
            Assert.That(auditAction.ResponseHeaders, Is.Not.Null);
            Assert.That(auditEventWebApi.Action, Is.EqualTo(auditAction));
        }

        [Test]
        public async Task AfterExecutedAsync_SetsAuditAction_WithExceptionAndNoResponse()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var exception = new InvalidOperationException("Test exception");
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, exception)
            {
                Response = null
            };

            var auditAction = new AuditApiAction();
            var auditEventWebApi = new AuditEventWebApi();
            var auditScopeMock = await AuditScope.CreateAsync(c => c.EventType("test").AuditEvent(auditEventWebApi));
            var contextWrapper = new Mock<IContextWrapper>();

            contextWrapper.Setup(x => x.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey)).Returns(auditAction);
            contextWrapper.Setup(x => x.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey)).Returns(auditScopeMock as AuditScope);
            
            await _adapter.AfterExecutedAsync(
                actionExecutedContext,
                contextWrapper.Object,
                includeModelState: false,
                includeResponseBody: false,
                includeResponseHeaders: false,
                CancellationToken.None);

            Assert.That(auditAction.ResponseStatusCode, Is.EqualTo(500));
            Assert.That(auditAction.ResponseStatus, Is.EqualTo("Internal Server Error"));
            Assert.That(auditAction.Exception, Is.Not.Null);
            Assert.That(auditEventWebApi.Action, Is.EqualTo(auditAction));
        }

        [Test]
        public void AfterExecutedAsync_DoesNothing_WhenAuditActionOrScopeIsNull()
        {
            var actionContext = GetHttpActionContext(typeof(ControllerTest), typeof(ControllerTest).GetMethod("Get"));
            var actionExecutedContext = new HttpActionExecutedContext(actionContext, null)
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
            };

            var contextWrapper = new Mock<IContextWrapper>();
            contextWrapper.Setup(x => x.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey)).Returns((AuditApiAction)null);
            contextWrapper.Setup(x => x.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey)).Returns((AuditScope)null);

            // Should not throw or call anything
            Assert.DoesNotThrowAsync(async () =>
            {
                await _adapter.AfterExecutedAsync(
                    actionExecutedContext,
                    contextWrapper.Object,
                    includeModelState: true,
                    includeResponseBody: true,
                    includeResponseHeaders: true,
                    CancellationToken.None);
            });
        }
        
        [Test]
        public void ToDictionary_HttpHeaders_ReturnsDictionary()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Header1", "Value1");
            request.Headers.Add("Header2", new[] { "Value2", "Value3" });

            var dict = AuditApiAdapter.ToDictionary(request.Headers);

            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.ContainsKey("Header1"), Is.True);
            Assert.That(dict["Header1"], Is.EqualTo("Value1"));
            Assert.That(dict.ContainsKey("Header2"), Is.True);
            Assert.That(dict["Header2"], Is.EqualTo("Value2, Value3"));
        }

        [Test]
        public void ToDictionary_HttpHeaders_Null_ReturnsNull()
        {
            var dict = AuditApiAdapter.ToDictionary((HttpHeaders)null);
            Assert.That(dict, Is.Null);
        }

        [Test]
        public void ToDictionary_NameValueCollection_ReturnsDictionary()
        {
            var nvc = new NameValueCollection();
            nvc.Add("Key1", "Value1");
            nvc.Add("Key2", "Value2");

            var dict = AuditApiAdapter.ToDictionary(nvc);

            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.ContainsKey("Key1"), Is.True);
            Assert.That(dict["Key1"], Is.EqualTo("Value1"));
            Assert.That(dict.ContainsKey("Key2"), Is.True);
            Assert.That(dict["Key2"], Is.EqualTo("Value2"));
        }

        [Test]
        public void ToDictionary_NameValueCollection_Null_ReturnsNull()
        {
            var dict = AuditApiAdapter.ToDictionary((NameValueCollection)null);
            Assert.That(dict, Is.Null);
        }
        
        private class ControllerTest : ApiController
        {
            public int Get()
            {
                return 1;
            }

            [AuditIgnore]
            public int GetIgnored()
            {
                return 1;
            }
        }

        private HttpActionExecutedContext GetHttpActionExecutedContext(Type controllerType, MethodInfo method, Exception ex = null)
        {
            var actionContext = GetHttpActionContext(controllerType, method);
            var httpActionExecutedContext = new HttpActionExecutedContext(actionContext, ex);
            return httpActionExecutedContext;
        }

        private HttpActionContext GetHttpActionContext(Type controllerType, MethodInfo method)
        {
            var controllerContext = new HttpControllerContext()
            {
                ControllerDescriptor = new HttpControllerDescriptor()
                {
                    ControllerName = "values",
                    ControllerType = controllerType,
                },
                Request = new HttpRequestMessage()
            };
            controllerContext.Request.Headers.Add("test-header", "header-value");
            var actionDescriptor = new CandidateHttpActionDescriptor_Test(new ReflectedHttpActionDescriptor()
            {
                MethodInfo = method,
                ActionBinding = new HttpActionBinding()
            })
            {
                ControllerDescriptor = controllerContext.ControllerDescriptor
            };
            var actionContext = new HttpActionContext()
            {
                ActionDescriptor = actionDescriptor,
                ControllerContext = controllerContext
            };

            return actionContext;
        }

        [AuditIgnore]
        private class ControllerTestIgnored : ApiController
        {
            public int Get()
            {
                return 1;
            }
        }
    }
}
#endif