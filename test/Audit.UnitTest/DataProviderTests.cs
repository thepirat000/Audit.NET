using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.UnitTest
{
    public class DataProviderTests
    {
        [Test]
        public void Test_DataProviderFactory()
        {
            GetProviderCount = 0;
            var options = new AuditScopeOptions(_ => _.DataProvider(GetProvider).CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd));
            Assert.That(GetProviderCount, Is.EqualTo(0));
            using (var scope = AuditScope.Create(options))
            {
                Assert.That(GetProviderCount, Is.EqualTo(1));
                scope.SetCustomField("custom", "value");
                scope.Save();
            }
            Assert.That(GetProviderCount, Is.EqualTo(1));
            options = new AuditScopeOptions(_ => _.DataProvider(GetProvider).CreationPolicy(EventCreationPolicy.Manual));
            using (var scope = new AuditScope(options))
            {
                Assert.That(GetProviderCount, Is.EqualTo(2));
                scope.Save();
                scope.Save();
            }
            Assert.That(GetProviderCount, Is.EqualTo(2));
            Audit.Core.Configuration.DataProviderFactory = GetProvider;
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                Assert.That(GetProviderCount, Is.EqualTo(3));
                scope.Discard();
            }
            Assert.That(GetProviderCount, Is.EqualTo(3));

            Audit.Core.Configuration.Setup().UseFactory(GetProvider);
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                Assert.That(GetProviderCount, Is.EqualTo(4));
                scope.Save();
            }
            Assert.That(GetProviderCount, Is.EqualTo(4));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_DataProvider_CancellationToken(bool cancel)
        {
            var cs = new CancellationTokenSource();
            var events_inserted = new List<AuditEvent>();
            var events_replaced = new List<AuditEvent>();
            bool tokenCancelled_inserted = !cancel;
            bool tokenCancelled_replaced = !cancel;

            var dp = new DynamicAsyncDataProvider(c => c.OnInsert(async (ev, ct) =>
            {
                events_inserted.Add(AuditEvent.FromJson(ev.ToJson()));
                tokenCancelled_inserted = ct.IsCancellationRequested;
                await Task.CompletedTask;
            }).OnReplace(async (id, ev, ct) =>
            {
                events_replaced.Add(AuditEvent.FromJson(ev.ToJson()));
                tokenCancelled_replaced = ct.IsCancellationRequested;
                await Task.CompletedTask;
            }));

            if (cancel)
            {
                cs.Cancel();
            }

            var options = new AuditScopeOptions()
            {
                DataProvider = dp,
                CreationPolicy = EventCreationPolicy.Manual,
                EventType = "Test",
                ExtraFields = new { f = 1 }
            };

            var scope = await AuditScope.CreateAsync(options, cs.Token);
            await scope.SaveAsync(cs.Token);

            scope.SetCustomField("f", 2);
            await scope.SaveAsync(cs.Token);

            Assert.That(tokenCancelled_inserted, Is.EqualTo(cancel));
            Assert.That(tokenCancelled_replaced, Is.EqualTo(cancel));
            Assert.That(events_inserted.Count, Is.EqualTo(1));
            Assert.That(events_replaced.Count, Is.EqualTo(1));
            Assert.That(events_inserted[0].CustomFields["f"].ToString(), Is.EqualTo("1"));
            Assert.That(events_replaced[0].CustomFields["f"].ToString(), Is.EqualTo("2"));
        }

        private static int GetProviderCount = 0;
        private AuditDataProvider GetProvider()
        {
            GetProviderCount++;
            return new NullDataProvider();
        }
    }

}
