#if TEXT_JSON
using NUnit.Framework;
using Newtonsoft.Json;
using Audit.Core;
using System.Collections.Generic;

namespace Audit.JsonNetAdapter.UnitTest
{
    public class JsonNewtonsoftAdapterTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.AuditDisabled = false;
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_JsonNewtonsoftAdapter()
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
            var evs = new List<string>();
            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter(settings)
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev.ToJson());
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "test" }))
            {
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Contains(@"""$type"":"));
        }
    }
}
#endif