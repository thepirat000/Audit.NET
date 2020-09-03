using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Integration.AspNetCore.Pages.Test;
using Audit.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Audit.Integration.AspNetCore
{
    public class MvcTests
    {
        private readonly int _port;
        public MvcTests(int port)
        {
            _port = port;
        }

        public async Task Test_MvcRazorPages_HappyPath()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "test"
            };

            var client = new HttpClient();
            //GET
            var getResult = await client.GetStringAsync($"http://localhost:{_port}/PageTest?name=TEST");

            //POST
            client.Dispose();
            var json = JsonConvert.SerializeObject(customer);
            client = new HttpClient();
            var result = await client.PostAsync($"http://localhost:{_port}/PageTest", new StringContent(json, Encoding.UTF8, "application/json"));

            var returnedCustomer = JsonConvert.DeserializeObject<Customer>(await result.Content.ReadAsStringAsync());
            Assert.AreEqual(customer.Name, returnedCustomer.Name);
            Assert.AreEqual(customer.Id, returnedCustomer.Id);
            
            Assert.AreEqual(2, insertEvs.Count);
            Assert.AreEqual(0, replaceEvs.Count);
            Assert.AreEqual("TEST", (insertEvs[0].Action.Model as JObject).Value<string>("Name"));
            Assert.AreEqual("TEST", insertEvs[0].Action.ActionParameters["name"]);
            Assert.AreEqual("GET", insertEvs[0].Action.HttpMethod);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ActionName);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ViewPath);
            Assert.AreEqual($"http://localhost:{_port}/PageTest?name=TEST", insertEvs[0].Action.RequestUrl);
            Assert.AreEqual($"PageResult", insertEvs[0].Action.ResponseBody.Type);
            Assert.AreEqual(200, insertEvs[0].Action.ResponseStatusCode);
            Assert.AreEqual("GET:/PageTest/Index", insertEvs[0].EventType);
            Assert.IsNotNull(insertEvs[0].Action.TraceId);
            Assert.IsNull(insertEvs[0].Action.Exception);

            Assert.AreEqual("POST", insertEvs[1].Action.HttpMethod);
            Assert.AreEqual("test", (insertEvs[1].Action.ActionParameters["customer"] as JObject).Value<string>("Name"));
            Assert.AreEqual("/PageTest/Index", insertEvs[1].Action.ActionName);
            Assert.AreEqual("/PageTest/Index", insertEvs[1].Action.ViewPath);
            Assert.AreEqual($"http://localhost:{_port}/PageTest", insertEvs[1].Action.RequestUrl);
            Assert.AreEqual($"JsonResult", insertEvs[1].Action.ResponseBody.Type);
            Assert.AreEqual(123, (insertEvs[1].Action.ResponseBody.Value as JObject).ToObject<Customer>().Id);
            Assert.AreEqual("test", (insertEvs[1].Action.ResponseBody.Value as JObject).ToObject<Customer>().Name);
            Assert.AreEqual(200, insertEvs[1].Action.ResponseStatusCode);
            Assert.AreEqual("POST:/PageTest/Index", insertEvs[1].EventType);
            Assert.IsNotNull(insertEvs[1].Action.TraceId);
            Assert.IsNull(insertEvs[1].Action.Exception);
        }

        public async Task Test_MvcRazorPages_Exception()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "ThrowException"
            };

            var c = new HttpClient();
            var json = JsonConvert.SerializeObject(customer);
            var result = await c.PostAsync($"http://localhost:{_port}/PageTest", new StringContent(json, Encoding.UTF8, "application/json"));

            
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(0, replaceEvs.Count);
            Assert.IsTrue(insertEvs[0].Action.ActionParameters.ContainsKey("customer"));
            Assert.AreEqual("POST", insertEvs[0].Action.HttpMethod);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ActionName);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ViewPath);
            Assert.AreEqual(500, insertEvs[0].Action.ResponseStatusCode);

            Assert.IsNotNull(insertEvs[0].Action.Exception);
            Assert.IsTrue(insertEvs[0].Action.Exception.Contains("TEST EXCEPTION"));
        }

        public async Task Test_MvcRazorPages_404()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditEventMvcAction>(JsonConvert.SerializeObject(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "404"
            };

            var c = new HttpClient();
            var json = JsonConvert.SerializeObject(customer);
            var result = await c.PostAsync($"http://localhost:{_port}/PageTest", new StringContent(json, Encoding.UTF8, "application/json"));


            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(0, replaceEvs.Count);
            Assert.AreEqual("404", ((dynamic)insertEvs[0].Action.ActionParameters["customer"]).Name.Value);
            Assert.AreEqual("POST", insertEvs[0].Action.HttpMethod);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ActionName);
            Assert.AreEqual("/PageTest/Index", insertEvs[0].Action.ViewPath);
            Assert.AreEqual(404, insertEvs[0].Action.ResponseStatusCode);

            Assert.IsNull(insertEvs[0].Action.Exception);
        }

        public async Task Test_Mvc_Ignore()
        {
            var insertEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev.GetMvcAuditAction());
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            var c = new HttpClient();
            var s1 = await c.GetStringAsync($"http://localhost:{_port}/mvc/ignoreme");
            var s2 = await c.GetStringAsync($"http://localhost:{_port}/mvc/ignoreparam?id=123&secret=pass");

            Assert.IsNotEmpty(s1);
            Assert.IsNotEmpty(s2);
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, insertEvs[0].ActionParameters.Count);
            Assert.AreEqual(123, insertEvs[0].ActionParameters["id"]);
        }

        public async Task Test_Mvc_AuditIgnoreAttribute_Middleware_Async()
        {
            // Action ignored via AuditIgnoreAttribute and handled by Middleware and GlobalFilter
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
            var s1 = await c.GetStringAsync($"http://localhost:{_port}/mvc/details/5?middleware=1");

            Assert.IsNotEmpty(s1);
            Assert.AreEqual(0, insertEvs.Count);
        }

        public async Task Test_Mvc_HappyPath_Async()
        {
            var insertEvs = new List<AuditAction>();
            var replaceEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditAction>(JsonConvert.SerializeObject(ev.GetMvcAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditAction>(JsonConvert.SerializeObject(ev.GetMvcAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var c = new HttpClient();
            var s = await c.GetStringAsync($"http://localhost:{_port}/test/mytitle");
            Assert.IsTrue(s.Contains("<h2>mytitle</h2>"));
            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(1, replaceEvs.Count);
            Assert.AreEqual(null, insertEvs[0].Model);
            Assert.AreEqual(@"{""Title"":""mytitle""}", replaceEvs[0].Model.ToString().Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        }

        public async Task Test_Mvc_Exception_Async()
        {
            var insertEvs = new List<AuditAction>();
            var replaceEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditAction>(JsonConvert.SerializeObject(ev.GetMvcAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditAction>(JsonConvert.SerializeObject(ev.GetMvcAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var c = new HttpClient();
            string s = null;
            try
            {
                s = await c.GetStringAsync($"http://localhost:{_port}/test/666");
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
    }
}