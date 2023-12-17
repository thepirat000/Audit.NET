using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Audit.WebApi;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Audit.AspNetCore.UnitTest
{
    [TestFixture]
    public class WebApiTests
    {
        private HttpClient _httpClient;
        private WebApplicationFactory<Program> _application;

        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Reset();
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _application = new WebApplicationFactory<Program>();
            _httpClient = _application
                .WithWebHostBuilder(b => b
                    .UseSolutionRelativeContentRoot("")
                    .UseSetting("IsWebApi", "true")
                )
                .CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _httpClient?.Dispose();
            _application?.Dispose();
        }

        [Test]
        [Order(0)]
        public async Task TestInitialize()
        {
            var s = await _httpClient.GetStringAsync("api/values");
            Assert.That(s, Is.EqualTo("[\"value1\",\"value2\"]"));
        }

        [Test]
        public async Task Test_WebApi_CreationPolicy_InsertOnStartInsertOnEnd()
        {
            var insertEvs = new List<AuditEventWebApi>();
            var updatedEvs = new List<AuditEventWebApi>();
            Audit.Core.Configuration.Setup()
                .IncludeActivityTrace()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                })
                .OnReplace((id, ev) => {
                    updatedEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(insertEvs.Count, Is.EqualTo(2));
            Assert.That(updatedEvs.Count, Is.EqualTo(0));
            Assert.That(await res.Content.ReadAsStringAsync(), Is.Not.Null);
            Assert.That(insertEvs[0].Action.ResponseStatus, Is.Null);
            Assert.That(insertEvs[1].Action.ResponseStatus, Is.EqualTo("OK"));
            Assert.That(insertEvs[0].Activity, Is.Not.Null);
            Assert.That(insertEvs[1].Activity, Is.Not.Null);
            Assert.That(insertEvs[0].Activity.TraceId, Is.Not.Empty);
            Assert.That(insertEvs[1].Activity.TraceId, Is.Not.Empty);
            Assert.That(insertEvs[1].Activity.TraceId, Is.EqualTo(insertEvs[0].Activity.TraceId));
        }

        [Test]
        public async Task Test_WebApi_CreationPolicy_InsertOnEnd()
        {
            var insertEvs = new List<AuditEventWebApi>();
            var updatedEvs = new List<AuditEventWebApi>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                })
                .OnReplace((id, ev) => {
                    updatedEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(updatedEvs.Count, Is.EqualTo(0));
            Assert.That(await res.Content.ReadAsStringAsync(), Is.Not.Null);
            Assert.That(insertEvs[0].Action.ResponseStatus, Is.EqualTo("OK"));
        }

        [Test]
        public async Task Test_WebApi_CreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var insertEvs = new List<AuditEventWebApi>();
            var updatedEvs = new List<AuditEventWebApi>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                })
                .OnReplace((id, ev) => {
                    updatedEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(updatedEvs.Count, Is.EqualTo(1));
            Assert.That(await res.Content.ReadAsStringAsync(), Is.Not.Null);
            Assert.That(insertEvs[0].Action.ResponseStatus, Is.Null);
            Assert.That(updatedEvs[0].Action.ResponseStatus, Is.EqualTo("OK"));
        }

        [Test]
        public async Task Test_WebApi_CreationPolicy_Manual()
        {
            var insertEvs = new List<AuditEventWebApi>();
            var updatedEvs = new List<AuditEventWebApi>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                })
                .OnReplace((id, ev) => {
                    updatedEvs.Add(AuditEvent.FromJson<AuditEventWebApi>(ev.ToJson()));
                }))
                .WithCreationPolicy(EventCreationPolicy.Manual);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(insertEvs.Count, Is.EqualTo(0));
            Assert.That(updatedEvs.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Test_WebApi_AuditApiAttributeOrder()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(await res.Content.ReadAsStringAsync(), Is.Not.Null);
            Assert.That(insertEvs[0].CustomFields["ScopeExists"], Is.EqualTo(true));
        }

        [Test]
        public async Task Test_WebApi_AuditApiGlobalAttributeOrder()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/GlobalAudit";
            
            var res = await _httpClient.PostAsync(url, new StringContent("{\"value\": \"def\"}", Encoding.UTF8, "application/json"));

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(await res.Content.ReadAsStringAsync(), Is.Not.Null);
            Assert.That(insertEvs[0].CustomFields["ScopeExists"], Is.EqualTo(true));
            Assert.That(insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext(), Is.Not.Null);
            Assert.That((insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName, Is.EqualTo("GlobalAudit"));
        }

        [Test]
        public async Task Test_WebApi_TestFromServiceIgnore()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/Values/TestFromServiceIgnore?t=test";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionParameters.Count, Is.EqualTo(1));
            Assert.That(action.ActionParameters["t"], Is.EqualTo("test"));
            Assert.That(action.ActionName, Is.EqualTo("TestFromServiceIgnore"));
        }

        [Test]
        public async Task Test_WebApi_ResponseHeaders_Attribute()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            var url = $"api/values/TestResponseHeadersAttribute?id=test";
            
            var res = await _httpClient.PostAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsFalse(action.IsMiddleware);
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.Headers == null || action.Headers.Count == 0, Is.True);
            Assert.That(action.ResponseHeaders.Count > 0, Is.True);
            Assert.That(action.ResponseHeaders["some-header-attr"], Is.EqualTo("test"));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext(), Is.Not.Null);
            Assert.That((insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName, Is.EqualTo("TestResponseHeadersAttribute"));
        }

        [Test]
        public async Task Test_WebApi_ResponseHeaders_GlobalFilter()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            var url = $"api/values/TestResponseHeadersGlobalFilter?id=test";
            
            var res = await _httpClient.PostAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsFalse(action.IsMiddleware);
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.Headers.Count > 0, Is.True);
            Assert.That(action.ResponseHeaders.Count > 0, Is.True);
            Assert.That(action.ResponseHeaders["some-header-global"], Is.EqualTo("test"));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
        }
        
        [Test]
        public async Task Test_WebApi_ResponseHeaders_Middleware()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            var url = $"api/values/TestResponseHeaders?id=test&middleware=yes";
            
            var res = await _httpClient.PostAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.IsMiddleware, Is.True);
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionName, Is.Null);
            Assert.That(action.ControllerName, Is.Null);
            Assert.That(action.Headers.Count > 0, Is.True);
            Assert.That(action.ResponseHeaders.Count > 0, Is.True);
            Assert.That(action.ResponseHeaders["some-header"], Is.EqualTo("test"));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
        }

        [Test]
        public async Task Test_WebApi_Middleware_Mix_Filter()
        {
            // test using both mw and af
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/PostMix?middleware=123";
            
            var rqBody = "{\"value\": \"mix\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "application/json"));

            var responseBody = await res.Content.ReadAsStringAsync();

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(responseBody, Is.EqualTo("mix"));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo("PostMix"));
            Assert.That(action.ControllerName, Is.EqualTo("Values"));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionParameters.Count, Is.EqualTo(2));
            Assert.That(action.Exception, Is.EqualTo(null));
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseBody.Value, Is.EqualTo("mix"));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Test_WebApi_Middleware_Exception()
        {
            // exception test using mw 
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/PostMiddleware?middleware=1";
            
            var rqBody = "{\"value\": \"666\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "application/json"));


            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo(null));
            Assert.That(action.ControllerName, Is.EqualTo(null));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionParameters, Is.EqualTo(null));
            Assert.That(action.Exception, Is.Not.Null);
            Assert.That(action.Exception.ToUpper().Contains("THIS IS A TEST EXCEPTION 666"), Is.True);
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseBody, Is.EqualTo(null));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task Test_WebApi_Middleware_WrongRoute()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/doesnotexists?middleware=1";
            
            var rqBody = "{\"value\": \"x\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo(null));
            Assert.That(action.ControllerName, Is.EqualTo(null));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionParameters, Is.EqualTo(null));
            Assert.That(action.Exception, Is.Null);
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseStatusCode, Is.EqualTo(404));
        }
        
        [Test]
        public async Task Test_WebApi_Middleware_WrongData()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/PostMiddleware?middleware=1";
            
            var rqBody = "{\"value\": \"123\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "unknown/unknown"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo(null));
            Assert.That(action.ControllerName, Is.EqualTo(null));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionParameters, Is.EqualTo(null));
            Assert.That(action.Exception, Is.Null);
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseStatusCode, Is.EqualTo(415));
        }

        [Test]
        public async Task Test_WebApi_Middleware_NoResponseBody()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestNormal?middleware=123&noresponsebody=1";
            
            var rqBody = "{\"value\": \"def\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo(null));
            Assert.That(action.ControllerName, Is.EqualTo(null));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.Exception, Is.EqualTo(null));
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseBody, Is.EqualTo(null));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Test_WebApi_Mix_IgnoreResponse()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestIgnoreResponse?middleware=1";
            
            var rqBody = "{\"value\": \"def\"}";
            var res = await _httpClient.PostAsync(url, new StringContent(rqBody, Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo("TestIgnoreResponse"));
            Assert.That(action.ControllerName, Is.EqualTo("Values"));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.Exception, Is.EqualTo(null));
            Assert.That(action.RequestBody.Value, Is.EqualTo(rqBody));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseBody.Type, Is.EqualTo("OkObjectResult"));
            Assert.That(action.ResponseBody.Value, Is.EqualTo(null));
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Test_WebApi_DoubleActionFilter()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/MoreValues?middleware=123";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.ActionName, Is.EqualTo("Get"));
            Assert.That(action.ControllerName, Is.EqualTo("MoreValues"));
            Assert.That(action.IsMiddleware, Is.EqualTo(true));
            Assert.That(action.HttpMethod, Is.EqualTo("GET"));
            Assert.That(action.Exception, Is.EqualTo(null));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
            Assert.That(action.ResponseBody, Is.Not.Null);
            Assert.That(action.ResponseStatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Test_WebApi_Middleware_Alone()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            
            var url = $"api/values/TestNormal?middleware=123";
            
            var res = await _httpClient.PostAsync(url, new StringContent("{\"value\": \"def\"}", Encoding.UTF8, "application/json"));

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.That(action.IsMiddleware, Is.True);
            Assert.That(action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(action.ActionName, Is.Null);
            Assert.That(action.ControllerName, Is.Null);
            Assert.That(action.Headers.Count > 0, Is.True);
            Assert.That(action.RequestBody.Value.ToString(), Is.EqualTo("{\"value\": \"def\"}"));
            Assert.That(action.ResponseBody.Value.ToString(), Is.EqualTo("hi"));
            Assert.That(action.RequestUrl.EndsWith(url), Is.True);
        }

        [Test]
        public async Task Test_WebApi_AuditIgnoreAttribute_Action_Async()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestIgnoreAction";
            
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await _httpClient.SendAsync(req);


            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(0));
        }
        
        [Test]
        public async Task Test_WebApi_AuditIgnoreAttribute_Mix_Middleware_Async()
        {
            // Action ignored via AuditIgnoreAttribute, but handled by both ActionFilter and Middleware
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestIgnoreAction?middleware=1";
            
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await _httpClient.SendAsync(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Test_WebApi_AuditIgnoreAttribute_Middleware_AuditIgnoreFilter_Async()
        {
            // Action ignored via AuditIgnoreAttribute, but handled by Middleware (using the AuditIgnoreActionFilter)
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values?middleware=1&ignorefilter=1";
            
            var res = await _httpClient.GetAsync(url);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(0));
        }
        
        [Test]
        public async Task Test_WebApi_AuditIgnoreAttribute_Param_Async()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestIgnoreParam?user=john&pass=secret";
            
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await _httpClient.SendAsync(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ActionParameters.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("user"), Is.True);
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("password"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().IsMiddleware);
            Assert.That(insertEvs[0].GetWebApiAuditAction().RequestUrl.EndsWith(url), Is.True);
        }

        [Test]
        public async Task Test_WebApi_FormCollectionLimit_Async()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var url = $"api/values/TestForm";
            var nvc = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a", "1"),
                new KeyValuePair<string, string>("b", "2")
            };
            
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            var res = await _httpClient.SendAsync(req);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ResponseStatusCode, Is.EqualTo(200));
            Assert.That(insertEvs[0].GetWebApiAuditAction().FormVariables, Is.Not.Null);

            nvc.Add(new KeyValuePair<string, string>("c", "3"));
            req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            res = await _httpClient.SendAsync(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(2));
            Assert.That(insertEvs[0].GetWebApiAuditAction().TraceId, Is.Not.Null);
            Assert.That(insertEvs[1].GetWebApiAuditAction().TraceId, Is.Not.Null);
            Assert.That(insertEvs[1].GetWebApiAuditAction().TraceId, Is.Not.EqualTo(insertEvs[0].GetWebApiAuditAction().TraceId));
            // Form should be null since the api is limited to 2
            Assert.That(insertEvs[1].GetWebApiAuditAction().FormVariables, Is.Null);
        }

        [Test]
        public async Task Test_WebApi_GlobalFilter_Async()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            
            var s = await _httpClient.PostAsync($"api/values/GlobalAudit", new StringContent("{\"value\": \"def\"}", Encoding.UTF8, "application/json"));


            Assert.That(s.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().RequestBody.Value, Is.EqualTo("{\"value\": \"def\"}"));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ActionParameters.Count, Is.EqualTo(0), "request should not be logged on action params because it's ignored");
            Assert.That(insertEvs[0].GetWebApiAuditAction().ResponseBody.Value, Is.EqualTo("def"));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ResponseStatusCode, Is.EqualTo(200));
            Assert.That(insertEvs[0].EventType, Is.EqualTo("POST.Values.GlobalAudit"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().IsMiddleware);
        }

        [Test]
        public async Task Test_WebApi_GlobalFilter_IgnoreResponse()
        {
            var insertEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(ev);
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            
            var s = await _httpClient.PostAsync($"api/values/TestIgnoreResponseFilter", new StringContent("{\"value\": \"def\"}", Encoding.UTF8, "application/json"));


            Assert.That(s.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().RequestBody.Value, Is.EqualTo("{\"value\": \"def\"}"));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ActionParameters.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].GetWebApiAuditAction().ResponseBody.Value, Is.Null);
            Assert.That(insertEvs[0].GetWebApiAuditAction().ResponseStatusCode, Is.EqualTo(200));
            Assert.That(insertEvs[0].EventType, Is.EqualTo("POST Values/TestIgnoreResponseFilter"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().IsMiddleware);
        }

        [Test]
        public async Task Test_WebApi_HappyPath_Async()
        {
            var insertEvs = new List<AuditApiAction>();
            var replaceEvs = new List<AuditApiAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            
            var s = await _httpClient.GetStringAsync($"api/values/10");
            Assert.That(s, Is.EqualTo("10"));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].ResponseBody, Is.EqualTo(null));
            Assert.That(replaceEvs[0].ResponseBody.Value, Is.EqualTo("10"));
            Assert.IsFalse(insertEvs[0].IsMiddleware);
        }

        [Test]
        public async Task Test_WebApi_Post_Async()
        {
            var insertEvs = new List<AuditApiAction>();
            var replaceEvs = new List<AuditApiAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            
            var s = await _httpClient.PostAsync($"api/values", new StringContent("{\"value\": \"abc\"}", Encoding.UTF8, "application/json"));
            Assert.That(s.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].RequestBody.Value, Is.EqualTo("{\"value\": \"abc\"}"));
            Assert.That(insertEvs[0].ResponseBody, Is.EqualTo(null));
            Assert.That(replaceEvs[0].ResponseBody.Value, Is.EqualTo("abc"));
            Assert.That(insertEvs[0].ResponseHeaders, Is.Null);
            Assert.That(replaceEvs[0].ResponseHeaders, Is.Null);
        }

        [Test]
        public async Task Test_WebApi_Exception_Async()
        {
            var insertEvs = new List<AuditApiAction>();
            var replaceEvs = new List<AuditApiAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            
            string s = null;
            try
            {
                s = await _httpClient.GetStringAsync($"api/values/666");
            }
            catch 
            {
            }
            finally
            {
            }
            Assert.That(s, Is.EqualTo(null));
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs[0].Exception.Contains("THIS IS A TEST EXCEPTION"), Is.True, "returned exception: " + replaceEvs[0].Exception);
            Assert.That(replaceEvs[0].Exception.Contains("at Audit.AspNetCore.UnitTest.Controllers.ValuesController"), Is.True, "stacktrace not found");
        }
        
        [Test]
        public async Task Test_WebApi_FilterResponseBody_Included()
        {
            var insertEvs = new List<AuditApiAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    return Guid.NewGuid();
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            

            try
            {
                var s = await _httpClient.GetStringAsync($"api/values/hi/142857");
                Assert.Fail("Should not be here");
            }
            catch (Exception)
            {
            }

            // should not log the response body
            await _httpClient.GetStringAsync($"api/values/hi/111");

            Assert.That(insertEvs.Count, Is.EqualTo(2));
            Assert.That(insertEvs[0].ResponseBody.Value, Is.EqualTo("this is a bad request test"));
            Assert.That(insertEvs[1].ResponseBody, Is.Null);
        }

        [Test]
        public async Task Test_WebApi_MultiMiddleware_Async()
        {
            var insertEvs = new List<AuditApiAction>();
            var replaceEvs = new List<AuditApiAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(1);
                    insertEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditApiAction>(JsonConvert.SerializeObject(ev.GetWebApiAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);


            _httpClient.DefaultRequestHeaders.Add("UseErrorHandler", "True");
            string s = null;
            try
            {
                s = await _httpClient.GetStringAsync($"api/values/666?middleware=1");
            }
            catch
            {
            }
            finally
            {
            }
            Assert.That(s, Is.EqualTo("ApiErrorHandlerMiddleware"));
            Assert.That(replaceEvs[0].Exception.Contains("THIS IS A TEST EXCEPTION"), Is.True, "returned exception: " + replaceEvs[0].Exception);
            Assert.That(replaceEvs[0].Exception.Contains("at Audit.AspNetCore.UnitTest.Controllers.ValuesController"), Is.True, "stacktrace not found");
        }

        [Test]
        public async Task Test_WebApi_GlobalFilter_SerializeParams_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            

            var customer = new Customer() {Id = 123, Name = "Test"};
            var s = await _httpClient.PostAsync($"api/values/TestSerializeParams", new StringContent(Configuration.JsonAdapter.Serialize(customer), Encoding.UTF8, "application/json"));

            var events = (Core.Configuration.DataProvider as InMemoryDataProvider).GetAllEventsOfType<AuditEventWebApi>();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Action.ActionParameters.Count, Is.EqualTo(1));
            Assert.That((events[0].Action.ActionParameters["customer"] as Customer)?.Id, Is.EqualTo(123));
        }

        [Test]
        public async Task Test_WebApi_GlobalFilter_DoNotSerializeParams_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            

            var customer = new Customer() { Id = 123, Name = "Test" };
            var s = await _httpClient.PostAsync($"api/values/TestDoNotSerializeParams", new StringContent(Configuration.JsonAdapter.Serialize(customer), Encoding.UTF8, "application/json"));

            var events = (Core.Configuration.DataProvider as InMemoryDataProvider).GetAllEventsOfType<AuditEventWebApi>();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Action.ActionParameters.Count, Is.EqualTo(1));
            Assert.That((events[0].Action.ActionParameters["customer"] as Customer)?.Id, Is.EqualTo(-1));
        }

        [Test]
        public async Task Test_WebApi_JsonPatch_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            
            var patchDoc = new JsonPatchDocument<Customer>();
            patchDoc.Replace(e => e.Name, "NewValue");
            var bodyJson = JsonConvert.SerializeObject(patchDoc);
            
            var result = await _httpClient.PatchAsync($"api/My/JsonPatch", new StringContent(bodyJson, Encoding.UTF8, "application/json-patch+json"));

            var events = (Configuration.DataProvider as InMemoryDataProvider)?.GetAllEventsOfType<AuditEventWebApi>();
            var eventJson = events?.FirstOrDefault()?.ToJson();
            var op = (events?[0].Action.ActionParameters.First().Value as JsonPatchDocument<Customer>)?.Operations[0];

            Assert.That(events, Is.Not.Null);
            Assert.That(eventJson, Is.Not.Null);
            Assert.That(op, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(op.value, Is.EqualTo("NewValue"));
        }
    }
}