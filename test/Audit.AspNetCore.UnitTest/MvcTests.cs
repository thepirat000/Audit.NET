using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Audit.AspNetCore.UnitTest
{
    [TestFixture]
    public class MvcTests
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
                    .UseSetting("IsMvc", "true"))
                .CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _httpClient?.Dispose();
            _application?.Dispose();
        }
        
        [Test]
        public async Task Test_MvcRazorPages_HappyPath()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "test"
            };
            
            //GET
            var getResult = await _httpClient.GetStringAsync($"PageTest?name=TEST");

            //POST
            var json = Configuration.JsonAdapter.Serialize(customer);
            
            var result = await _httpClient.PostAsync($"PageTest", new StringContent(json, Encoding.UTF8, "application/json"));

            var returnedCustomer = Configuration.JsonAdapter.Deserialize<Customer>(await result.Content.ReadAsStringAsync());
            Assert.That(returnedCustomer.Name, Is.EqualTo(customer.Name));
            Assert.That(returnedCustomer.Id, Is.EqualTo(customer.Id));
            Assert.That(getResult.Contains("<p>TEST</p>"), Is.True);

            Assert.That(insertEvs.Count, Is.EqualTo(2));
            Assert.That(replaceEvs.Count, Is.EqualTo(0));
            Assert.That(((JsonElement)insertEvs[0].Action.Model).GetProperty("Name").GetString(), Is.EqualTo("TEST"));
            Assert.That(insertEvs[0].Action.ActionParameters["name"].ToString(), Is.EqualTo("TEST"));
            Assert.That(insertEvs[0].Action.HttpMethod, Is.EqualTo("GET"));
            Assert.That(insertEvs[0].Action.ActionName, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.ViewPath, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.RequestUrl.EndsWith("PageTest?name=TEST"), Is.True);
            Assert.That(insertEvs[0].Action.ResponseBody.Type, Is.EqualTo($"PageResult"));
            Assert.That(insertEvs[0].Action.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(insertEvs[0].EventType, Is.EqualTo("GET:/PageTest/Index"));
            Assert.That(insertEvs[0].Action.TraceId, Is.Not.Null);
            Assert.That(insertEvs[0].Action.Exception, Is.Null);

            Assert.That(insertEvs[1].Action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(((JsonElement)insertEvs[1].Action.ActionParameters["customer"]).GetProperty("Name").GetString(), Is.EqualTo("test"));
            Assert.That(insertEvs[1].Action.ActionName, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[1].Action.ViewPath, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[1].Action.RequestUrl.EndsWith("PageTest"), Is.True);
            Assert.That(insertEvs[1].Action.ResponseBody.Type, Is.EqualTo($"JsonResult"));
            Assert.That(((JsonElement)insertEvs[1].Action.ResponseBody.Value).GetProperty("Id").GetInt32(), Is.EqualTo(123));
            Assert.That(((JsonElement)insertEvs[1].Action.ResponseBody.Value).GetProperty("Name").GetString(), Is.EqualTo("test"));
            Assert.That(insertEvs[1].Action.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(insertEvs[1].EventType, Is.EqualTo("POST:/PageTest/Index"));
            Assert.That(insertEvs[1].Action.TraceId, Is.Not.Null);
            Assert.That(insertEvs[1].Action.Exception, Is.Null);
        }

        [Test]
        public async Task Test_MvcRazorPages_Exception()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "ThrowException"
            };

            
            var json = Configuration.JsonAdapter.Serialize(customer);
            var result = await _httpClient.PostAsync($"PageTest", new StringContent(json, Encoding.UTF8, "application/json"));


            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(0));
            Assert.That(insertEvs[0].Action.ActionParameters.ContainsKey("customer"), Is.True);
            Assert.That(insertEvs[0].Action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(insertEvs[0].Action.ActionName, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.ViewPath, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.ResponseStatusCode, Is.EqualTo(500));

            Assert.That(insertEvs[0].Action.Exception, Is.Not.Null);
            Assert.That(insertEvs[0].Action.Exception.Contains("TEST EXCEPTION"), Is.True);
        }

        [Test]
        public async Task Test_MvcRazorPages_404()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "404"
            };

            
            var json = Configuration.JsonAdapter.Serialize(customer);
            var result = await _httpClient.PostAsync($"PageTest", new StringContent(json, Encoding.UTF8, "application/json"));


            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(0));
            Assert.That(Configuration.JsonAdapter.ToObject<Customer>(insertEvs[0].Action.ActionParameters["customer"]).Name, Is.EqualTo("404"));
            Assert.That(insertEvs[0].Action.HttpMethod, Is.EqualTo("POST"));
            Assert.That(insertEvs[0].Action.ActionName, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.ViewPath, Is.EqualTo("/PageTest/Index"));
            Assert.That(insertEvs[0].Action.ResponseStatusCode, Is.EqualTo(404));

            Assert.That(insertEvs[0].Action.Exception, Is.Null);
        }

        [Test]
        public async Task Test_MvcRazorPages_IgnoreResponse()
        {
            var insertEvs = new List<AuditEventMvcAction>();
            var replaceEvs = new List<AuditEventMvcAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    await Task.Delay(0);
                    insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    return Guid.NewGuid();
                })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(0);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditEventMvcAction>(Configuration.JsonAdapter.Serialize(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var customer = new Customer()
            {
                Id = 123,
                Name = "test"
            };

            //PUT
            var json = Configuration.JsonAdapter.Serialize(customer);
            var result = await _httpClient.PutAsync($"PageTest", new StringContent(json, Encoding.UTF8, "application/json"));
            var resultJson = await result.Content.ReadAsStringAsync();
            var returnedCustomer = Configuration.JsonAdapter.Deserialize<Customer>(resultJson);
            Assert.That(returnedCustomer.Name, Is.EqualTo(customer.Name));
            Assert.That(returnedCustomer.Id, Is.EqualTo(customer.Id));

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(0));
            Assert.That(insertEvs[0].Action.ResponseBody.Type, Is.EqualTo("JsonResult"));
            Assert.That(insertEvs[0].Action.ResponseBody.Value, Is.Null);
        }

        [Test]
        public async Task Test_Mvc_Ignore()
        {
            var insertEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    insertEvs.Add(ev.GetMvcAuditAction());
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            
            var s1 = await _httpClient.GetStringAsync($"mvc/ignoreme");
            var s2 = await _httpClient.GetStringAsync($"mvc/ignoreparam?id=123&secret=pass");
            var s3 = await _httpClient.GetStringAsync($"mvc/ignoreresponse?id=123&secret=pass");

            Assert.That(s1, Is.Not.Empty);
            Assert.That(s2, Is.Not.Empty);
            Assert.That(insertEvs.Count, Is.EqualTo(2));
            Assert.That(insertEvs[0].ActionParameters.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].ActionParameters["id"].ToString(), Is.EqualTo("123"));
            Assert.That(insertEvs[0].ResponseBody.Value, Is.Not.Null);

            Assert.That(s3, Is.Not.Null);
            Assert.That(insertEvs[1].ResponseBody.Value, Is.Null);
        }

        [Test]
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

            
            var s1 = await _httpClient.GetStringAsync($"mvc/details/5?middleware=1");

            Assert.That(s1, Is.Not.Empty);
            Assert.That(insertEvs.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Test_Mvc_HappyPath_Async()
        {
            var insertEvs = new List<AuditAction>();
            var replaceEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditAction>(Configuration.JsonAdapter.Serialize(ev.GetMvcAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditAction>(Configuration.JsonAdapter.Serialize(ev.GetMvcAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            
            var s = await _httpClient.GetStringAsync($"test/mytitle");
            Assert.That(s.Contains("<h2>mytitle</h2>"), Is.True);
            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(1));
            Assert.That(insertEvs[0].Model, Is.EqualTo(null));
            Assert.That(replaceEvs[0].Model.ToString().Replace(" ", "").Replace("\r", "").Replace("\n", ""), Is.EqualTo(@"{""Title"":""mytitle""}"));
        }

        [Test]
        public async Task Test_Mvc_Exception_Async()
        {
            var insertEvs = new List<AuditAction>();
            var replaceEvs = new List<AuditAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(Configuration.JsonAdapter.Deserialize<AuditAction>(Configuration.JsonAdapter.Serialize(ev.GetMvcAuditAction())));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(Configuration.JsonAdapter.Deserialize<AuditAction>(Configuration.JsonAdapter.Serialize(ev.GetMvcAuditAction())));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string s = null;
            try
            {
                s = await _httpClient.GetStringAsync($"test/666");
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
        }
    }
}