using Audit.Elasticsearch.Providers;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Audit.Core;
using System.Threading;
using System.Threading.Tasks;
using Audit.IntegrationTest;

namespace Audit.Elasticsearch.UnitTest
{
    public class ElasticsearchTests
    {
        private ElasticsearchDataProvider GetElasticsearchDataProvider(List<Core.AuditEvent> ins, List<Core.AuditEvent> repl)
        {
            var client = new ElasticClient(new Uri(AzureSettings.ElasticSearchUrl));
            return new ElasticsearchDataProviderForTest(ins, repl, client);
        }

        [Test]
        public void Test_ElasticSearchDataProvider_FluentApi()
        {
            var x = new Elasticsearch.Providers.ElasticsearchDataProvider(_ => _
                .ConnectionSettings(new ConnectionSettings(new Uri("http://server/")))
                .Id(ev => "id")
                .Index("ix"));

            Assert.That((x.ConnectionSettings.ConnectionPool.Nodes.First().Uri.ToString()), Is.EqualTo("http://server/"));
            Assert.That(x.IdBuilder.Invoke(null).Equals(new Nest.Id("id")), Is.True);
            Assert.That(x.IndexBuilder.Invoke(null).Name, Is.EqualTo("ix"));
        }
        
        [Test]
        [Category("Integration")]
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
            var orderOldValue = Core.Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.Old.ToString());
            var orderNewValue = Core.Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.New.ToString());
            var oldDictionary = evLoad.Target.Old as Dictionary<string, object>;
            var newDictionary = evLoad.Target.New as Dictionary<string, object>;

            Assert.That(evLoad, Is.Not.Null);
            Assert.That(oldDictionary, Is.Not.Null);
            Assert.That(newDictionary, Is.Not.Null);
            Assert.That(guids.Count, Is.EqualTo(1));
            Assert.That(ins.Count, Is.EqualTo(1));
            Assert.That(repl.Count, Is.EqualTo(1));
            Assert.That(orderOldValue.Status, Is.EqualTo("Created"));
            Assert.That(oldDictionary["status"].ToString(), Is.EqualTo("Created"));
            Assert.That(orderNewValue.Status, Is.EqualTo("Updated"));
            Assert.That(newDictionary["status"].ToString(), Is.EqualTo("Updated"));
            Assert.That(evLoad.CustomFields["MyCustomField"], Is.EqualTo("value"));
            Assert.That(ins[0].Target.New, Is.EqualTo(null));
        }

        [Test]
        [Category("Integration")]
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
            var orderOldValue = Core.Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.Old.ToString());
            var orderNewValue = Core.Configuration.JsonAdapter.Deserialize<Order>(repl[0].Target.New.ToString());
            var oldDictionary = evLoad.Target.Old as Dictionary<string, object>;
            var newDictionary = evLoad.Target.New as Dictionary<string, object>;

            Assert.That(evLoad, Is.Not.Null);
            Assert.That(oldDictionary, Is.Not.Null);
            Assert.That(newDictionary, Is.Not.Null);
            Assert.That(guids.Count, Is.EqualTo(1));
            Assert.That(ins.Count, Is.EqualTo(1));
            Assert.That(repl.Count, Is.EqualTo(1));
            Assert.That(orderOldValue.Status, Is.EqualTo("Created"));
            Assert.That(oldDictionary["status"].ToString(), Is.EqualTo("Created"));
            Assert.That(orderNewValue.Status, Is.EqualTo("Updated"));
            Assert.That(newDictionary["status"].ToString(), Is.EqualTo("Updated"));
            Assert.That(evLoad.CustomFields["MyCustomField"], Is.EqualTo("value"));
            Assert.That(ins[0].Target.New, Is.EqualTo(null));
        }

        [Test]
        [Category("Integration")]
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

            Assert.That(evResult, Is.Not.Null);
            Assert.That(results.Documents.Count, Is.EqualTo(1));
            Assert.That(ins.Count, Is.EqualTo(1));
            Assert.That(repl.Count, Is.EqualTo(1));
            Assert.That(evResult.Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(ins[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(ins[0].Target.New, Is.EqualTo(null));
            Assert.That(repl[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(repl[0].Target.New.ToString(), Is.EqualTo("init-end"));
            Assert.That(evResult.Target.New.ToString(), Is.EqualTo("init-end"));
            Assert.That(evResult.CustomFields["MyCustomField"]?.ToString(), Is.EqualTo("value"));
        }

        [Test]
        [Category("Integration")]
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

            Assert.That(evResult, Is.Not.Null);
            Assert.That(results.Documents.Count, Is.EqualTo(1));
            Assert.That(ins.Count, Is.EqualTo(1));
            Assert.That(repl.Count, Is.EqualTo(1));
            Assert.That(evResult.Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(ins[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(ins[0].Target.New, Is.EqualTo(null));
            Assert.That(repl[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(repl[0].Target.New.ToString(), Is.EqualTo("init-end"));
            Assert.That(evResult.Target.New.ToString(), Is.EqualTo("init-end"));
            Assert.That(evResult.CustomFields["MyCustomField"]?.ToString(), Is.EqualTo("value"));
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

    public class Order
    {
        public virtual long Id { get; set; }
        public virtual string Number { get; set; }
        public virtual string Status { get; set; }
    }
}
