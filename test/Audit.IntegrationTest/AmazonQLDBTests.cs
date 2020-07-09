#if NET461 || NETCOREAPP2_0 || NETCOREAPP2_1
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Audit.Core;
using System.Threading.Tasks;
using Amazon.QLDB.Driver;
using Amazon.QLDBSession;
using Audit.NET.AmazonQLDB.Providers;

namespace Audit.IntegrationTest
{
    public class AmazonQLDBTests
    {
        private AmazonQldbDataProvider GetAmazonQLDBDataProvider(List<AuditEvent> ins, List<AuditEvent> repl) =>
            new AmazonQldbDataProviderForTest(ins, repl)
            {
                QldbDriver = new Lazy<IQldbDriver>(() => QldbDriver.Builder()
                    .WithQLDBSessionConfig(new AmazonQLDBSessionConfig())
                    .WithLedger("AuditEvent")
                    .WithRetryLogging()
                    .WithMaxConcurrentTransactions(2)
                    .Build()),
                TableNameBuilder = ev => ev.EventType
            };

        [Test]
        [Category("AmazonQLDB")]
        public void Test_AmazonQLDB_HappyPath()
        {
            var ins = new List<AuditEvent>();
            var repl = new List<AuditEvent>();
            var qldb = GetAmazonQLDBDataProvider(ins, repl);

            Configuration.Setup()
                .UseCustomProvider(qldb)
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var sb = "init";

            using (var scope = AuditScope.Create("eventType", () => sb, new { MyCustomField = "value" }))
            {
                sb += "-end";
            }

            Assert.AreEqual(1, ins.Count);
            Assert.AreEqual(1, repl.Count);
            Assert.AreEqual("init", ins[0].Target.Old);
            Assert.AreEqual(null, ins[0].Target.New);
            Assert.AreEqual("init", repl[0].Target.Old);
            Assert.AreEqual("init-end", repl[0].Target.New);
        }
    }

    public class AmazonQldbDataProviderForTest : AmazonQldbDataProvider
    {
        private List<AuditEvent> _inserted;
        private List<AuditEvent> _replaced;

        public AmazonQldbDataProviderForTest(List<AuditEvent> ins, List<AuditEvent> repl)
        {
            _inserted = ins;
            _replaced = repl;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            _inserted.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEvent(auditEvent);
        }
        public override Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            _inserted.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEventAsync(auditEvent);
        }
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            _replaced.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            base.ReplaceEvent(eventId, auditEvent);
        }
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            _replaced.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            return base.ReplaceEventAsync(eventId, auditEvent);
        }
    }
}
#endif