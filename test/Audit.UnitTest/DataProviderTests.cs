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
            Assert.AreEqual(0, GetProviderCount);
            using (var scope = AuditScope.Create(options))
            {
                Assert.AreEqual(1, GetProviderCount);
                scope.SetCustomField("custom", "value");
                scope.Save();
            }
            Assert.AreEqual(1, GetProviderCount);
            options = new AuditScopeOptions(_ => _.DataProvider(GetProvider).CreationPolicy(EventCreationPolicy.Manual));
            using (var scope = new AuditScope(options))
            {
                Assert.AreEqual(2, GetProviderCount);
                scope.Save();
                scope.Save();
            }
            Assert.AreEqual(2, GetProviderCount);
            Audit.Core.Configuration.DataProviderFactory = GetProvider;
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            { 
                Assert.AreEqual(3, GetProviderCount);
                scope.Discard();
            }
            Assert.AreEqual(3, GetProviderCount);

            Audit.Core.Configuration.Setup().UseFactory(GetProvider);
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                Assert.AreEqual(4, GetProviderCount);
                scope.Save();
            }
            Assert.AreEqual(4, GetProviderCount);
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

            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(c => c.OnInsert(async (ev, ct) =>
                {
                    events_inserted.Add(AuditEvent.FromJson(ev.ToJson()));
                    tokenCancelled_inserted = ct.IsCancellationRequested;
                    await Task.CompletedTask;
                }).OnReplace(async (id, ev, ct) =>
                {
                    events_replaced.Add(AuditEvent.FromJson(ev.ToJson()));
                    tokenCancelled_replaced = ct.IsCancellationRequested;
                    await Task.CompletedTask;
                }))
                .WithCreationPolicy(EventCreationPolicy.Manual);

            if (cancel)
            {
                cs.Cancel();
            }

            var scope = await AuditScope.CreateAsync("Test", null, new { f = 1 }, cs.Token);
            await scope.SaveAsync(cs.Token);

            scope.SetCustomField("f", 2);
            await scope.SaveAsync(cs.Token);

            Assert.AreEqual(cancel, tokenCancelled_inserted);
            Assert.AreEqual(cancel, tokenCancelled_replaced);
            Assert.AreEqual(1, events_inserted.Count);
            Assert.AreEqual(1, events_replaced.Count);
            Assert.AreEqual("1", events_inserted[0].CustomFields["f"].ToString());
            Assert.AreEqual("2", events_replaced[0].CustomFields["f"].ToString());
        }

        private static int GetProviderCount = 0;
        private AuditDataProvider GetProvider()
        {
            GetProviderCount++;
            return new NullDataProvider();
        }
    }

}
