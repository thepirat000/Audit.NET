#if NET461 || NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
using Audit.Elasticsearch.Providers;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var indexName = "auditevent_order";
            
            var guids = new List<string>();
            ela.IndexBuilder = ev => indexName;
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var order = new Order()
            {
                Id = 1,
                Status = "Created"
            };
            

            using (var scope = new AuditScopeFactory().Create("eventType", () => order, new { MyCustomField = "value" }, null, null))
            {
                order.Status = "Updated";
            }

            ela.Client.Indices.Refresh(indexName);

            var evLoad = ela.GetEvent(new ElasticsearchAuditEventId() { Id = guids[0], Index = indexName });
            var orderOldValue = Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.Old.ToString());
            var orderNewValue = Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.New.ToString());
            var oldDictionary = evLoad.Target.Old as Dictionary<string, object>;
            var newDictionary = evLoad.Target.New as Dictionary<string, object>;

            Assert.IsNotNull(evLoad);
            Assert.IsNotNull(oldDictionary);
            Assert.IsNotNull(newDictionary);
            Assert.AreEqual(1, guids.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("Created", orderOldValue.Status);
            Assert.AreEqual("Created", oldDictionary["status"].ToString());
            Assert.AreEqual("Updated", orderNewValue.Status);
            Assert.AreEqual("Updated", newDictionary["status"].ToString());
            Assert.AreEqual("value", evLoad.CustomFields["MyCustomField"]);
            Assert.AreEqual(null, ins[0].Target.New);
        }

        [Test]
        [Category("Elasticsearch")]
        public async Task Test_Elasticsearch_HappyPath_Async()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetElasticsearchDataProvider(ins, repl);
            var indexName = "auditevent_order";

            var guids = new List<string>();
            ela.IndexBuilder = ev => indexName;
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var order = new Order()
            {
                Id = 1,
                Status = "Created"
            };

            using (var scope = await new AuditScopeFactory().CreateAsync("eventType", () => order, new { MyCustomField = "value" }, null, null))
            {
                order.Status = "Updated";
            }

            await ela.Client.Indices.RefreshAsync(indexName);

            var evLoad = await ela.GetEventAsync(new ElasticsearchAuditEventId() { Id = guids[0], Index = indexName });
            var orderOldValue = Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.Old.ToString());
            var orderNewValue = Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.New.ToString());
            var oldDictionary = evLoad.Target.Old as Dictionary<string, object>;
            var newDictionary = evLoad.Target.New as Dictionary<string, object>;

            Assert.IsNotNull(evLoad);
            Assert.IsNotNull(oldDictionary);
            Assert.IsNotNull(newDictionary);
            Assert.AreEqual(1, guids.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("Created", orderOldValue.Status);
            Assert.AreEqual("Created", oldDictionary["status"].ToString());
            Assert.AreEqual("Updated", orderNewValue.Status);
            Assert.AreEqual("Updated", newDictionary["status"].ToString());
            Assert.AreEqual("value", evLoad.CustomFields["MyCustomField"]);
            Assert.AreEqual(null, ins[0].Target.New);
        }

        [Test]
        [Category("Elasticsearch")]
        public void Test_Elasticsearch_AutoGeneratedId()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetElasticsearchDataProvider(ins, repl);
            var indexName = "auto_" + new Random().Next(10000, 99999);

            ela.IndexBuilder = ev => indexName;
            ela.IdBuilder = ev => null;

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";


            using (var scope = new AuditScopeFactory().Create("eventType", () => sb, new { MyCustomField = "value" }, null, null))
            {
                sb += "-end";
            }

            ela.Client.Indices.Refresh(indexName);

            var results = ela.Client.Search<Core.AuditEvent>(new SearchRequest(indexName));
            var evResult = results.Documents.FirstOrDefault();
            if (evResult != null)
            {
                ela.Client.Delete(new DeleteRequest(results.Hits.First().Index, results.Hits.First().Id));
            }

            Assert.IsNotNull(evResult);
            Assert.AreEqual(1, results.Documents.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("init", evResult.Target.Old.ToString());
            Assert.AreEqual("init", ins[0].Target.Old.ToString());
            Assert.AreEqual(null, ins[0].Target.New);
            Assert.AreEqual("init", repl[0].Target.Old.ToString());
            Assert.AreEqual("init-end", repl[0].Target.New.ToString());
            Assert.AreEqual("init-end", evResult.Target.New.ToString());
            Assert.AreEqual("value", evResult.CustomFields["MyCustomField"]?.ToString());
        }

        [Test]
        [Category("Elasticsearch")]
        public async Task Test_Elasticsearch_AutoGeneratedId_Async()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetElasticsearchDataProvider(ins, repl);
            var indexName = "auto_" + new Random().Next(10000, 99999);

            ela.IndexBuilder = ev => indexName;
            ela.IdBuilder = ev => null;

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";


            using (var scope = await new AuditScopeFactory().CreateAsync("eventType", () => sb, new { MyCustomField = "value" }, null, null))
            {
                sb += "-end";
            }

            await ela.Client.Indices.RefreshAsync(indexName);

            var results = await ela.Client.SearchAsync<Core.AuditEvent>(new SearchRequest(indexName));
            var evResult = results.Documents.FirstOrDefault();
            if (evResult != null)
            {
                await ela.Client.DeleteAsync(new DeleteRequest(results.Hits.First().Index, results.Hits.First().Id));
            }

            Assert.IsNotNull(evResult);
            Assert.AreEqual(1, results.Documents.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("init", evResult.Target.Old.ToString());
            Assert.AreEqual("init", ins[0].Target.Old.ToString());
            Assert.AreEqual(null, ins[0].Target.New);
            Assert.AreEqual("init", repl[0].Target.Old.ToString());
            Assert.AreEqual("init-end", repl[0].Target.New.ToString());
            Assert.AreEqual("init-end", evResult.Target.New.ToString());
            Assert.AreEqual("value", evResult.CustomFields["MyCustomField"]?.ToString());
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
        public override Task<object> InsertEventAsync(Core.AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _inserted.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEventAsync(auditEvent, cancellationToken);
        }
        public override void ReplaceEvent(object eventId, Core.AuditEvent auditEvent)
        {
            _replaced.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            base.ReplaceEvent(eventId, auditEvent);
        }
        public override Task ReplaceEventAsync(object eventId, Core.AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _replaced.Add(Audit.Core.AuditEvent.FromJson(auditEvent.ToJson()));
            return base.ReplaceEventAsync(eventId, auditEvent, cancellationToken);
        }
    }
}
#endif