using Audit.Elasticsearch.Providers;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Audit.Core;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.UnitTest
{
    public class ElasticsearchTests
    {
        private ElasticsearchDataProvider GetMockElasticsearchDataProvider(List<Core.AuditEvent> insertedList, List<Core.AuditEvent> replacedList)
        {
            var client = new Mock<IElasticClient>();
            // setup Create
            client.Setup(_ => _.Create<Core.AuditEvent>(It.IsAny<ICreateRequest<Core.AuditEvent>>())) //CreateRequest<AuditEvent>
                .Returns<ICreateRequest<Core.AuditEvent>>(req =>
                {
                    var icreateresponse = new Mock<ICreateResponse>();
                    icreateresponse.SetupGet(_ => _.IsValid).Returns(true);
                    icreateresponse.SetupGet(_ => _.Result).Returns(Result.Created);
                    icreateresponse.SetupGet(_ => _.Index).Returns(req.Index?.ToString());
                    icreateresponse.SetupGet(_ => _.Id).Returns(req.Id?.ToString());
                    icreateresponse.SetupGet(_ => _.Type).Returns(req.Type?.ToString());
                    insertedList.Add(Core.AuditEvent.FromJson(req.Document.ToJson()));
                    return icreateresponse.Object;
                });

            // setup Index
            client.Setup(_ => _.Index<Core.AuditEvent>(It.IsAny<IIndexRequest<Core.AuditEvent>>()))
                .Returns<IIndexRequest<Core.AuditEvent>>(req =>
                {
                    var iindexresponse = new Mock<IIndexResponse>();
                    iindexresponse.SetupGet(_ => _.IsValid).Returns(true);
                    iindexresponse.SetupGet(_ => _.Result).Returns(Result.Updated);
                    iindexresponse.SetupGet(_ => _.Index).Returns(req.Index?.ToString());
                    iindexresponse.SetupGet(_ => _.Id).Returns(req.Id?.ToString());
                    iindexresponse.SetupGet(_ => _.Type).Returns(req.Type?.ToString());
                    replacedList.Add(Core.AuditEvent.FromJson(req.Document.ToJson()));
                    return iindexresponse.Object;
                });

            // setup CreateAsync
            client.Setup(_ => _.CreateAsync<Core.AuditEvent>(It.IsAny<ICreateRequest<Core.AuditEvent>>(), It.IsAny<CancellationToken>()))
                .Returns((ICreateRequest<Core.AuditEvent> req, CancellationToken cr) =>
                {
                    var icreateresponse = new Mock<ICreateResponse>();
                    icreateresponse.SetupGet(_ => _.IsValid).Returns(true);
                    icreateresponse.SetupGet(_ => _.Result).Returns(Result.Created);
                    icreateresponse.SetupGet(_ => _.Index).Returns(req.Index?.ToString());
                    icreateresponse.SetupGet(_ => _.Id).Returns(req.Id?.ToString());
                    icreateresponse.SetupGet(_ => _.Type).Returns(req.Type?.ToString());
                    insertedList.Add(Core.AuditEvent.FromJson(req.Document.ToJson()));
                    return Task.FromResult(icreateresponse.Object);
                });

            // setup IndexAsync
            client.Setup(_ => _.IndexAsync<Core.AuditEvent>(It.IsAny<IIndexRequest<Core.AuditEvent>>(), It.IsAny<CancellationToken>()))
                .Returns((IIndexRequest<Core.AuditEvent> req, CancellationToken cr) =>
                {
                    var iindexresponse = new Mock<IIndexResponse>();
                    iindexresponse.SetupGet(_ => _.IsValid).Returns(true);
                    iindexresponse.SetupGet(_ => _.Result).Returns(Result.Updated);
                    iindexresponse.SetupGet(_ => _.Index).Returns(req.Index?.ToString());
                    iindexresponse.SetupGet(_ => _.Id).Returns(req.Id?.ToString());
                    iindexresponse.SetupGet(_ => _.Type).Returns(req.Type?.ToString());
                    replacedList.Add(Core.AuditEvent.FromJson(req.Document.ToJson()));
                    return Task.FromResult(iindexresponse.Object);
                });

            return new ElasticsearchDataProvider(client.Object);
        }

        [Test]
        public void Test_Elastic_HappyPath()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetMockElasticsearchDataProvider(ins, repl);

            var guids = new List<string>();
            ela.IndexBuilder = ev => "auditevent";
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            ela.TypeNameBuilder = ev => "spe/c/ial";

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
            Assert.AreEqual("init", ins[0].Target.SerializedOld);
            Assert.AreEqual(null, ins[0].Target.SerializedNew);
            Assert.AreEqual("init", repl[0].Target.SerializedOld);
            Assert.AreEqual("init-end", repl[0].Target.SerializedNew);
        }

        [Test]
        public async Task Test_Elastic_HappyPath_Async()
        {
            var ins = new List<Core.AuditEvent>();
            var repl = new List<Core.AuditEvent>();
            var ela = GetMockElasticsearchDataProvider(ins, repl);

            var guids = new List<string>();
            ela.IndexBuilder = ev => "auditevent";
            ela.IdBuilder = ev => { var g = Guid.NewGuid().ToString().Replace("-", "/"); guids.Add(g); return g; };

            ela.TypeNameBuilder = ev => "spe/c/ial";

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(ela)
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";


            using (var scope = await AuditScope.CreateAsync("eventType", () => sb, new { MyCustomField = "value" }))
            {
                sb += "-end";
                await scope.DisposeAsync();
            }

            Assert.AreEqual(1, guids.Count);
            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("init", ins[0].Target.SerializedOld);
            Assert.AreEqual(null, ins[0].Target.SerializedNew);
            Assert.AreEqual("init", repl[0].Target.SerializedOld);
            Assert.AreEqual("init-end", repl[0].Target.SerializedNew);
        }

    }
}
