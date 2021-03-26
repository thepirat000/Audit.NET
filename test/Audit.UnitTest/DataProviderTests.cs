using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

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

        private static int GetProviderCount = 0;
        private AuditDataProvider GetProvider()
        {
            GetProviderCount++;
            return new NullDataProvider();
        }
    }

}
