#if NK_JSON
using NUnit.Framework;
using System.Text.Json;
using Audit.Core;
using System.Collections.Generic;
using System.Linq;

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
        public void Test_JsonSystemAdapter()
        {
            var settings = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var evs = new List<string>();
            Audit.Core.Configuration.Setup()
                .JsonSystemAdapter(settings)
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev.ToJson());
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "test" }))
            {
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Count(c => c == '\n') > 5);
            Assert.IsTrue(evs[0].Contains(@"""environment"""));
        }
    }
}
#endif
