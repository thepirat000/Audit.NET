﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.QLDB.Driver;
using Amazon.QLDBSession;
using Audit.Core;
using Audit.AmazonQLDB.Providers;
using NUnit.Framework;

namespace Audit.AmazonQLDB.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("AmazonQLDB")]
    public class AmazonQLDBTests
    {
        private AmazonQldbDataProvider GetAmazonQLDBDataProvider(List<AuditEvent> ins, List<AuditEvent> repl) =>
            new AmazonAsyncQldbDataProviderForTest(ins, repl)
            {
                QldbDriver = new Lazy<IAsyncQldbDriver>(() => AsyncQldbDriver.Builder()
                    .WithQLDBSessionConfig(new AmazonQLDBSessionConfig())
                    .WithLedger("audit-ledger")
                    .WithRetryLogging()
                    .WithMaxConcurrentTransactions(2)
                    .Build()),
                TableName = new Setting<string>(ev => ev.EventType)
            };

        [Test]
        [Category("Integration")]
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

            using (var scope = new AuditScopeFactory().Create("test_table", () => sb, new { MyCustomField = "value" }, null, null))
            {
                sb += "-end";
            }

            Assert.That(ins.Count, Is.EqualTo(1));
            Assert.That(repl.Count, Is.EqualTo(1));
            Assert.That(ins[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(ins[0].Target.New, Is.EqualTo(null));
            Assert.That(repl[0].Target.Old.ToString(), Is.EqualTo("init"));
            Assert.That(repl[0].Target.New.ToString(), Is.EqualTo("init-end"));
        }
    }

    public class AmazonAsyncQldbDataProviderForTest : AmazonQldbDataProvider
    {
        private List<AuditEvent> _inserted;
        private List<AuditEvent> _replaced;

        public AmazonAsyncQldbDataProviderForTest(List<AuditEvent> ins, List<AuditEvent> repl)
        {
            _inserted = ins;
            _replaced = repl;
        }

        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _inserted.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            return base.InsertEventAsync(auditEvent, cancellationToken);
        }

        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _replaced.Add(AuditEvent.FromJson(auditEvent.ToJson()));
            return base.ReplaceEventAsync(eventId, auditEvent, cancellationToken);
        }
    }
}