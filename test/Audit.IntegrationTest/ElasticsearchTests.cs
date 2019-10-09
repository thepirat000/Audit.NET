#if NET461 || NETCOREAPP2_0 || NETCOREAPP2_1
using Audit.Elasticsearch.Providers;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Audit.Core;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using System.Text;

namespace Audit.IntegrationTest
{
    public class ElasticsearchTests
    {
        private ElasticsearchDataProvider GetElasticsearchDataProvider(List<Core.AuditEvent> ins, List<Core.AuditEvent> repl)
        {
            var client = new ElasticClient(new Uri("http://127.0.0.1:9200"));
            return new ElasticsearchDataProviderForTest(ins, repl, client);
        }

        [Test]
        [Category("Elasticsearch")]
        public void Test_Elasticsearch_HappyPath()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetElasticsearchDataProvider(ins, repl);

            var guids = new List<string>();
            ela.IndexBuilder = ev => "auditevent2";
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";
            

            using (var scope = AuditScope.Create("eventType", () => sb, new { MyCustomField = "value" }))
            {
                sb += "-end";
            }

            Assert.AreEqual(1, guids.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("\"init\"", ins[0].Target.SerializedOld);
            Assert.AreEqual(null, ins[0].Target.SerializedNew);
            Assert.AreEqual("\"init\"", repl[0].Target.SerializedOld);
            Assert.AreEqual("\"init-end\"", repl[0].Target.SerializedNew);
        }

        [Test]
        [Category("Elasticsearch")]
        public async Task Test_Elasticsearch_HappyPath_Async()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetElasticsearchDataProvider(ins, repl);

            var guids = new List<string>();
            ela.IndexBuilder = ev => "auditevent2";
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";


            using (var scope = await AuditScope.CreateAsync("eventType", () => sb, new { MyCustomField = "value" }))
            {
                sb += "-end";
            }

            Assert.AreEqual(1, guids.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("\"init\"", ins[0].Target.SerializedOld);
            Assert.AreEqual(null, ins[0].Target.SerializedNew);
            Assert.AreEqual("\"init\"", repl[0].Target.SerializedOld);
            Assert.AreEqual("\"init-end\"", repl[0].Target.SerializedNew);
        }
    }

    public class ElasticsearchDataProviderForTest : ElasticsearchDataProvider
    {
        private List<Core.AuditEvent> _inserted;
        private List<Core.AuditEvent> _replaced;

        public ElasticsearchDataProviderForTest(List<Core.AuditEvent> ins, List<Core.AuditEvent> repl, IElasticClient cli) : base(cli)
        {
            _inserted = ins;
            _replaced = repl;
        }

        public override object InsertEvent(Core.AuditEvent auditEvent)
        {
            _inserted.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEvent(auditEvent);
        }
        public override Task<object> InsertEventAsync(Core.AuditEvent auditEvent)
        {
            _inserted.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEventAsync(auditEvent);
        }
        public override void ReplaceEvent(object eventId, Core.AuditEvent auditEvent)
        {
            _replaced.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            base.ReplaceEvent(eventId, auditEvent);
        }
        public override Task ReplaceEventAsync(object eventId, Core.AuditEvent auditEvent)
        {
            _replaced.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            return base.ReplaceEventAsync(eventId, auditEvent);
        }
    }
}
#endif