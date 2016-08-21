using Audit.Core;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audit.UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestSave()
        {
            var provider = A.Fake<AuditDataProvider>();
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).ReturnsLazily(s => s.ToString());
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).CallsBaseMethod();
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            var target = "initial";
            var eventType = "SomeEvent";
            var refId = "123";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, refId))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
            }
            Assert.AreEqual(refId, ev.ReferenceId);
            Assert.AreEqual(eventType, ev.EventType);
            Assert.IsTrue(ev.Comments.Contains("test"));
            Assert.IsTrue(ev.Target.SerializedOld.ToString().Contains("initial"));
            Assert.IsTrue(ev.Target.SerializedNew.ToString().Contains("final"));
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(3));
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void TestDiscard()
        {
            var provider = A.Fake<AuditDataProvider>();
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).ReturnsLazily(s => s.ToString());
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).CallsBaseMethod();
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            var target = "initial";
            var eventType = "SomeEvent";
            var refId = "123";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, refId))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Discard();
            }
            Assert.AreEqual(refId, ev.ReferenceId);
            Assert.AreEqual(eventType, ev.EventType);
            Assert.IsTrue(ev.Comments.Contains("test"));
            Assert.IsTrue(ev.Target.SerializedOld.ToString().Contains("initial"));
            Assert.IsNull(ev.Target.SerializedNew);
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(2));
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
        }

        [TestMethod]
        public void Test_InsertOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            provider.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).CallsBaseMethod();
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", "123"))
            {
                scope.Comment("test");
                scope.Save();
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Test_InsertOnStartReplaceOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            provider.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).CallsBaseMethod();
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", "123"))
            {
                scope.Comment("test");
                scope.Save();
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Test_InsertOnStartInsertOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            provider.CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd;
            A.CallTo(() => provider.Init(A<AuditEvent>.Ignored)).CallsBaseMethod();
            A.CallTo(() => provider.End(A<AuditEvent>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", "123"))
            {
                scope.Comment("test");
                scope.Save();
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Twice);
        }
    }
}
