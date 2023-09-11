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

namespace Audit.Integration.AspNetCore
{
    [TestFixture]
    public class WebApiTests
    {
        private HttpClient _httpClient;
        private WebApplicationFactory<Program> _application;

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
            Assert.AreEqual("[\"value1\",\"value2\"]", s);
        }

        [Test]
        public async Task Test_WebApi_CreationPolicy_InsertOnStartInsertOnEnd()
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
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var url = "api/My";
            
            var res = await _httpClient.GetAsync(url);

            Assert.AreEqual(2, insertEvs.Count);
            Assert.AreEqual(0, updatedEvs.Count);
            Assert.IsNotNull(await res.Content.ReadAsStringAsync());
            Assert.IsNull(insertEvs[0].Action.ResponseStatus);
            Assert.AreEqual("OK", insertEvs[1].Action.ResponseStatus);
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

            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(0, updatedEvs.Count);
            Assert.IsNotNull(await res.Content.ReadAsStringAsync());
            Assert.AreEqual("OK", insertEvs[0].Action.ResponseStatus);
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

            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, updatedEvs.Count);
            Assert.IsNotNull(await res.Content.ReadAsStringAsync());
            Assert.IsNull(insertEvs[0].Action.ResponseStatus);
            Assert.AreEqual("OK", updatedEvs[0].Action.ResponseStatus);
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

            Assert.AreEqual(0, insertEvs.Count);
            Assert.AreEqual(0, updatedEvs.Count);
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

            Assert.AreEqual(1, insertEvs.Count);
            Assert.IsNotNull(await res.Content.ReadAsStringAsync());
            Assert.AreEqual(true, insertEvs[0].CustomFields["ScopeExists"]);
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

            Assert.AreEqual(1, insertEvs.Count);
            Assert.IsNotNull(await res.Content.ReadAsStringAsync());
            Assert.AreEqual(true, insertEvs[0].CustomFields["ScopeExists"]);
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext());
            Assert.AreEqual("GlobalAudit", (insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual(1, action.ActionParameters.Count);
            Assert.AreEqual("test", action.ActionParameters["t"]);
            Assert.AreEqual("TestFromServiceIgnore", action.ActionName);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsFalse(action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.IsTrue(action.Headers == null || action.Headers.Count == 0);
            Assert.IsTrue(action.ResponseHeaders.Count > 0);
            Assert.AreEqual("test", action.ResponseHeaders["some-header-attr"]);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext());
            Assert.AreEqual("TestResponseHeadersAttribute", (insertEvs[0].GetWebApiAuditAction().GetActionExecutingContext().ActionDescriptor as ControllerActionDescriptor).ActionName);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsFalse(action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.IsTrue(action.Headers.Count > 0);
            Assert.IsTrue(action.ResponseHeaders.Count > 0);
            Assert.AreEqual("test", action.ResponseHeaders["some-header-global"]);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsTrue(action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.IsNull(action.ActionName);
            Assert.IsNull(action.ControllerName);
            Assert.IsTrue(action.Headers.Count > 0);
            Assert.IsTrue(action.ResponseHeaders.Count > 0);
            Assert.AreEqual("test", action.ResponseHeaders["some-header"]);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual("mix", responseBody);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual("PostMix", action.ActionName);
            Assert.AreEqual("Values", action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(2, action.ActionParameters.Count);
            Assert.AreEqual(null, action.Exception);
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual("mix", action.ResponseBody.Value);
            Assert.AreEqual(200, action.ResponseStatusCode);
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


            Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual(null, action.ActionName);
            Assert.AreEqual(null, action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(null, action.ActionParameters);
            Assert.IsNotNull(action.Exception);
            Assert.IsTrue(action.Exception.ToUpper().Contains("THIS IS A TEST EXCEPTION 666"));
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual(null, action.ResponseBody);
            Assert.AreEqual(500, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual(null, action.ActionName);
            Assert.AreEqual(null, action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(null, action.ActionParameters);
            Assert.IsNull(action.Exception);
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual(404, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual(null, action.ActionName);
            Assert.AreEqual(null, action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(null, action.ActionParameters);
            Assert.IsNull(action.Exception);
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual(415, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual(null, action.ActionName);
            Assert.AreEqual(null, action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(null, action.Exception);
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual(null, action.ResponseBody);
            Assert.AreEqual(200, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual("TestIgnoreResponse", action.ActionName);
            Assert.AreEqual("Values", action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.AreEqual(null, action.Exception);
            Assert.AreEqual(rqBody, action.RequestBody.Value);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.AreEqual("OkObjectResult", action.ResponseBody.Type);
            Assert.AreEqual(null, action.ResponseBody.Value);
            Assert.AreEqual(200, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.AreEqual("Get", action.ActionName);
            Assert.AreEqual("MoreValues", action.ControllerName);
            Assert.AreEqual(true, action.IsMiddleware);
            Assert.AreEqual("GET", action.HttpMethod);
            Assert.AreEqual(null, action.Exception);
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
            Assert.IsNotNull(action.ResponseBody);
            Assert.AreEqual(200, action.ResponseStatusCode);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            var action = insertEvs[0].GetWebApiAuditAction();
            Assert.IsTrue(action.IsMiddleware);
            Assert.AreEqual("POST", action.HttpMethod);
            Assert.IsNull(action.ActionName);
            Assert.IsNull(action.ControllerName);
            Assert.IsTrue(action.Headers.Count > 0);
            Assert.AreEqual("{\"value\": \"def\"}", action.RequestBody.Value.ToString());
            Assert.AreEqual("hi", action.ResponseBody.Value.ToString());
            Assert.IsTrue(action.RequestUrl.EndsWith(url));
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


            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(0, insertEvs.Count);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(0, insertEvs.Count);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(0, insertEvs.Count);
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

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, insertEvs[0].GetWebApiAuditAction().ActionParameters.Count);
            Assert.IsTrue(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("user"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("password"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().IsMiddleware);
            Assert.IsTrue(insertEvs[0].GetWebApiAuditAction().RequestUrl.EndsWith(url));
        }

        [Test]
        public async Task Test_WebApi_Audit_Action_DiscardIfNot()
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

            var url = $"api/values/TestDiscardStatusCode";

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await _httpClient.SendAsync(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.AreEqual(0, insertEvs.Count);
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
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(200, insertEvs[0].GetWebApiAuditAction().ResponseStatusCode);
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().FormVariables);

            nvc.Add(new KeyValuePair<string, string>("c", "3"));
            req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            res = await _httpClient.SendAsync(req);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(2, insertEvs.Count);
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().TraceId);
            Assert.IsNotNull(insertEvs[1].GetWebApiAuditAction().TraceId);
            Assert.AreNotEqual(insertEvs[0].GetWebApiAuditAction().TraceId, insertEvs[1].GetWebApiAuditAction().TraceId);
            // Form should be null since the api is limited to 2
            Assert.IsNull(insertEvs[1].GetWebApiAuditAction().FormVariables);
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


            Assert.AreEqual(HttpStatusCode.OK, s.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual("{\"value\": \"def\"}", insertEvs[0].GetWebApiAuditAction().RequestBody.Value);
            Assert.AreEqual(0, insertEvs[0].GetWebApiAuditAction().ActionParameters.Count, "request should not be logged on action params because it's ignored");
            Assert.AreEqual("def", insertEvs[0].GetWebApiAuditAction().ResponseBody.Value);
            Assert.AreEqual(200, insertEvs[0].GetWebApiAuditAction().ResponseStatusCode);
            Assert.AreEqual("POST.Values.GlobalAudit", insertEvs[0].EventType);
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


            Assert.AreEqual(HttpStatusCode.OK, s.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual("{\"value\": \"def\"}", insertEvs[0].GetWebApiAuditAction().RequestBody.Value);
            Assert.AreEqual(1, insertEvs[0].GetWebApiAuditAction().ActionParameters.Count);
            Assert.IsNull(insertEvs[0].GetWebApiAuditAction().ResponseBody.Value);
            Assert.AreEqual(200, insertEvs[0].GetWebApiAuditAction().ResponseStatusCode);
            Assert.AreEqual("POST Values/TestIgnoreResponseFilter", insertEvs[0].EventType);
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
            Assert.AreEqual("10", s);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.AreEqual(null, insertEvs[0].ResponseBody);
            Assert.AreEqual("10", replaceEvs[0].ResponseBody.Value);
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
            Assert.AreEqual(HttpStatusCode.OK, s.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.AreEqual("{\"value\": \"abc\"}", insertEvs[0].RequestBody.Value);
            Assert.AreEqual(null, insertEvs[0].ResponseBody);
            Assert.AreEqual("abc", replaceEvs[0].ResponseBody.Value);
            Assert.IsNull(insertEvs[0].ResponseHeaders);
            Assert.IsNull(replaceEvs[0].ResponseHeaders);
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
            Assert.AreEqual(null, s);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.IsTrue(replaceEvs[0].Exception.Contains("THIS IS A TEST EXCEPTION"), "returned exception: " + replaceEvs[0].Exception);
            Assert.IsTrue(replaceEvs[0].Exception.Contains("at Audit.Integration.AspNetCore.Controllers.ValuesController"), "stacktrace not found");
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

            Assert.AreEqual(2, insertEvs.Count);
            Assert.AreEqual("this is a bad request test", insertEvs[0].ResponseBody.Value);
            Assert.IsNull(insertEvs[1].ResponseBody);
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
            Assert.AreEqual("ApiErrorHandlerMiddleware", s);
            Assert.IsTrue(replaceEvs[0].Exception.Contains("THIS IS A TEST EXCEPTION"), "returned exception: " + replaceEvs[0].Exception);
            Assert.IsTrue(replaceEvs[0].Exception.Contains("at Audit.Integration.AspNetCore.Controllers.ValuesController"), "stacktrace not found");
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
            
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(1, events[0].Action.ActionParameters.Count);
            Assert.AreEqual(123, (events[0].Action.ActionParameters["customer"] as Customer)?.Id);
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

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(1, events[0].Action.ActionParameters.Count);
            Assert.AreEqual(-1, (events[0].Action.ActionParameters["customer"] as Customer)?.Id);
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
            
            Assert.IsNotNull(events);
            Assert.IsNotNull(eventJson);
            Assert.IsNotNull(op);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("NewValue", op.value);
        }
    }
}