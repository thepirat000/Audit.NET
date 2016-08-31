using Audit.Core;
using System;
using Xunit;
using Moq;
using Audit.Core.Providers;

namespace Audit.UnitTest
{
    public class UnitTest
    {
        [Fact]
        public void TestSave()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            AuditConfiguration.SetDataProvider(provider.Object);
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Save(); // this should do nothing because of the creation policy
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            }
            Assert.Equal(eventType, ev.EventType);
            Assert.True(ev.Comments.Contains("test"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
        public void TestDiscard()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            AuditConfiguration.SetDataProvider(provider.Object);
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = AuditScope.Create(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Discard();
            }
            Assert.Equal(eventType, ev.EventType);
            Assert.True(ev.Comments.Contains("test"));
            Assert.Null(ev.Target.SerializedNew);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
        }

        [Fact]
        public void Test_EventCreationPolicy_InsertOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider.Object);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnEnd))
            {
                scope.Comment("test");
                scope.Save(); // this should do nothing because of the creation policy
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
        public void Test_EventCreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider.Object);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
        public void Test_EventCreationPolicy_InsertOnStartInsertOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditConfiguration.SetDataProvider(provider.Object);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [Fact]
        public void Test_EventCreationPolicy_Manual()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            AuditConfiguration.SetDataProvider(provider.Object);
            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);

            using (var scope = AuditScope.Create("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
                scope.Save();
                scope.Comment("test2");
                scope.Save();
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
        public void Test_ExtraFields()
        {
            AuditConfiguration.SetDataProvider(new FileDataProvider());
            var scope = AuditScope.Create("SomeEvent", null, new { @class = "class value", DATA = 123 }, EventCreationPolicy.Manual);
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.Equal("123", ev.CustomFields["DATA"].ToString());
            Assert.Equal("class value", ev.CustomFields["class"].ToString());
        }

        [Fact]
        public void Test_TwoScopes()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            AuditConfiguration.SetDataProvider(provider.Object);
            var scope1 = AuditScope.Create("SomeEvent1", null, new { @class = "class value1", DATA = 111 }, EventCreationPolicy.Manual);
            scope1.Save();
            var scope2 = AuditScope.Create("SomeEvent2", null, new { @class = "class value2", DATA = 222 }, EventCreationPolicy.Manual);
            scope2.Save();
            Assert.NotNull(scope1.Event.EventId);
            Assert.NotNull(scope2.Event.EventId);
            Assert.NotEqual(scope1.Event.EventId, scope2.Event.EventId);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

    }
}
