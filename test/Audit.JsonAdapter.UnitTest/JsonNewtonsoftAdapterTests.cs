using NUnit.Framework;
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Contains(@"""$type"":"), Is.True);
            Assert.IsFalse(evs[0].Contains(@"""CustomFields"""));
            Assert.That(evs[0].Contains(@"""ExtraField"""), Is.True);
            Assert.That(deserialized.CustomFields["ExtraField"].ToString(), Is.EqualTo("123"));
        }
    }
}