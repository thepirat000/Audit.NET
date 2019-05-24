using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Mvc;
using Newtonsoft.Json;
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