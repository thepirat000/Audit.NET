﻿using NUnit.Framework;
using Newtonsoft.Json;
using Audit.Core;
using System.Collections.Generic;

namespace Audit.JsonAdapter.UnitTest
{
    public class JsonNewtonsoftAdapterTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_JsonNewtonsoftAdapter()
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All
            };
            var evs = new List<string>();
            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter(settings)
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev.ToJson());
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "test", ExtraFields = new { ExtraField = 123 } } ))
            {
            }

            var deserialized = AuditEvent.FromJson(evs[0]);

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Contains(@"""$type"":"));
            Assert.IsFalse(evs[0].Contains(@"""CustomFields"""));
            Assert.IsTrue(evs[0].Contains(@"""ExtraField"""));
            Assert.AreEqual("123", deserialized.CustomFields["ExtraField"].ToString());
        }
    }
}