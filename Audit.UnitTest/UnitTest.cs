using System;
using System.Runtime.InteropServices;
using Audit.Core;
using Audit.Core.Providers;
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
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            var target = "initial";
            var eventType = "SomeEvent";
            var refId = "123";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Save(); // this should do nothing because of the creation policy
                A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
            }
            Assert.AreEqual(eventType, ev.EventType);
            Assert.IsTrue(ev.Comments.Contains("test"));
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void TestDiscard()
        {
            var provider = A.Fake<AuditDataProvider>();
            A.CallTo(() => provider.Serialize(A<string>.Ignored)).CallsBaseMethod();
            AuditConfiguration.SetDataProvider(provider);
            var target = "initial";
            var eventType = "SomeEvent";
            var refId = "123";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Discard();
            }
            Assert.AreEqual(eventType, ev.EventType);
            Assert.IsTrue(ev.Comments.Contains("test"));
            Assert.IsNull(ev.Target.SerializedNew);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);
        }

        [TestMethod]
        public void Test_EventCreationPolicy_InsertOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnEnd))
            {
                scope.Comment("test");
                scope.Save(); // this should do nothing because of the creation policy
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored))
                .MustHaveHappened(Repeated.Never);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Test_EventCreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd))
            {
                scope.Comment("test");
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Test_EventCreationPolicy_InsertOnStartInsertOnEnd()
        {
            var provider = A.Fake<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd))
            {
                scope.Comment("test");
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored))
                .MustHaveHappened(Repeated.Never);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Twice);
        }

        [TestMethod]
        public void Test_EventCreationPolicy_Manual()
        {
            var provider = A.Fake<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
            }
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Never);

            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
                scope.Save();
                scope.Comment("test2");
                scope.Save();
            }
            A.CallTo(() => provider.ReplaceEvent(A<object>.Ignored, A<AuditEvent>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Test_ExtraFields()
        {
            AuditConfiguration.SetDataProvider(new FileDataProvider());
            var scope = AuditScope.Create("SomeEvent", null, new {@class = "class value", DATA = 123});
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.AreEqual("123", ev.CustomFields["DATA"].ToString());
            Assert.AreEqual("class value", ev.CustomFields["class"].ToString());
        }

        [TestMethod]
        public void Test_TwoScopes()
        {
            var provider = A.Fake<AuditDataProvider>();
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).ReturnsLazily(() => Guid.NewGuid());
            AuditConfiguration.SetDataProvider(provider);
            var scope1 = AuditScope.Create("SomeEvent1", null, new {@class = "class value1", DATA = 111}, EventCreationPolicy.Manual);
            scope1.Save();
            var scope2 = AuditScope.Create("SomeEvent2", null, new {@class = "class value2", DATA = 222}, EventCreationPolicy.Manual);
            scope2.Save();
            Assert.IsNotNull(scope1.Event.EventId);
            Assert.IsNotNull(scope2.Event.EventId);
            Assert.AreNotEqual(scope1.Event.EventId, scope2.Event.EventId);
            A.CallTo(() => provider.InsertEvent(A<AuditEvent>.Ignored)).MustHaveHappened(Repeated.Exactly.Twice);
        }
    }
}