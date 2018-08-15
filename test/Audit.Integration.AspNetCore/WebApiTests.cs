using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.WebApi;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Audit.Integration.AspNetCore
{
    public class WebApiTests
    {
        private readonly int _port;
        public WebApiTests(int port)
        {
            _port = port;
        }

        public async Task TestInitialize()
        {
            var c = new HttpClient();
            var s = await c.GetStringAsync($"http://localhost:{_port}/api/values");
            Assert.AreEqual("[\"value1\",\"value2\"]", s);
        }

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

            var url = $"http://localhost:{_port}/api/values/TestIgnoreAction";
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await client.SendAsync(req);


            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(0, insertEvs.Count);
        }

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

            var url = $"http://localhost:{_port}/api/values/TestIgnoreParam?user=john&pass=secret";
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var res = await client.SendAsync(req);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, insertEvs[0].GetWebApiAuditAction().ActionParameters.Count);
            Assert.IsTrue(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("user"));
            Assert.IsFalse(insertEvs[0].GetWebApiAuditAction().ActionParameters.ContainsKey("password"));
        }

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

            var url = $"http://localhost:{_port}/api/values/TestForm";
            var nvc = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a", "1"),
                new KeyValuePair<string, string>("b", "2")
            };


            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            var res = await client.SendAsync(req);
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(200, insertEvs[0].GetWebApiAuditAction().ResponseStatusCode);
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().FormVariables);

            nvc.Add(new KeyValuePair<string, string>("c", "3"));
            req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            res = await client.SendAsync(req);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(2, insertEvs.Count);
            Assert.IsNotNull(insertEvs[0].GetWebApiAuditAction().TraceId);
            Assert.IsNotNull(insertEvs[1].GetWebApiAuditAction().TraceId);
            Assert.AreNotEqual(insertEvs[0].GetWebApiAuditAction().TraceId, insertEvs[1].GetWebApiAuditAction().TraceId);
            // Form should be null since the api is limited to 2
            Assert.IsNull(insertEvs[1].GetWebApiAuditAction().FormVariables);
        }

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

            var c = new HttpClient();
            var s = await c.PostAsync($"http://localhost:{_port}/api/values/GlobalAudit", new StringContent("{\"value\": \"def\"}", Encoding.UTF8, "application/json"));


            Assert.AreEqual(HttpStatusCode.OK, s.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual("{\"value\": \"def\"}", insertEvs[0].GetWebApiAuditAction().RequestBody.Value);
            Assert.AreEqual(0, insertEvs[0].GetWebApiAuditAction().ActionParameters.Count, "request should not be logged on action params because it's ignored");
            Assert.AreEqual("def", insertEvs[0].GetWebApiAuditAction().ResponseBody.Value);
            Assert.AreEqual(200, insertEvs[0].GetWebApiAuditAction().ResponseStatusCode);
            Assert.AreEqual("POST.Values.GlobalAudit", insertEvs[0].EventType);
        }

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

            var c = new HttpClient();
            var s = await c.GetStringAsync($"http://localhost:{_port}/api/values/10");
            Assert.AreEqual("10", s);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.AreEqual(null, insertEvs[0].ResponseBody);
            Assert.AreEqual("10", replaceEvs[0].ResponseBody.Value);
        }

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

            var c = new HttpClient();
            var s = await c.PostAsync($"http://localhost:{_port}/api/values", new StringContent("{\"value\": \"abc\"}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.OK, s.StatusCode);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.AreEqual("{\"value\": \"abc\"}", insertEvs[0].RequestBody.Value);
            Assert.AreEqual(null, insertEvs[0].ResponseBody);
            Assert.AreEqual("abc", replaceEvs[0].ResponseBody.Value);
        }

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

            var c = new HttpClient();
            string s = null;
            try
            {
                s = await c.GetStringAsync($"http://localhost:{_port}/api/values/666");
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
        }


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

            var c = new HttpClient();

            try
            {
                var s = await c.GetStringAsync($"http://localhost:{_port}/api/values/hi/142857");
                Assert.Fail("Should not be here");
            }
            catch (Exception)
            {
            }

            // should not log the response body
            await c.GetStringAsync($"http://localhost:{_port}/api/values/hi/111");

            Assert.AreEqual(2, insertEvs.Count);
            Assert.AreEqual("this is a bad request test", insertEvs[0].ResponseBody.Value);
            Assert.IsNull(insertEvs[1].ResponseBody);
        }
    }
}