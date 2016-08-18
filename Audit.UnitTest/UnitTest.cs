using System;
using System.Runtime.ConstrainedExecution;
using Audit.Core;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audit.UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestDiscard()
        {
            var provider = A.Fake<IAuditDataProvider>();
            A.CallTo(() => provider.TestConnection()).Returns(true);
            AuditConfiguration.SetDataProvider(provider);
            var scope = AuditScope.Create("SomeEvent", () => "target", "123");
            scope.Comment("test");
            scope.Discard();
            scope.Save();
            scope.Dispose();
            A.CallTo(() => provider.WriteEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
        }
        [TestMethod]
        public void TestWrite()
        {
            var provider = A.Fake<IAuditDataProvider>();
            A.CallTo(() => provider.TestConnection()).Returns(true);
            AuditConfiguration.SetDataProvider(provider);
            var scope = AuditScope.Create("SomeEvent", () => "target", "123");
            scope.Comment("test");
            scope.Save();
            scope.Save();
            scope.Dispose();
            A.CallTo(() => provider.WriteEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
