using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Audit.Core.Providers.Wrappers;
using Moq;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ConditionalDataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public void Test_InsertEvent_WhenOtherwise(string eventType)
        {
            // Arrange
            var auditEvent = new AuditEvent() { EventType = eventType };

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.InsertEvent(auditEvent)).Returns((object)null);

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.InsertEvent(auditEvent)).Returns((object)null);

            var dp_3 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_3.Setup(x => x.InsertEvent(auditEvent)).Returns((object)null);

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1.Object)
                .When(ev => ev.EventType == "B", dp_2.Object)
                .Otherwise(dp_3.Object));
            
            // Act
            conditionalDataProvider.InsertEvent(auditEvent);

            // Assert
            dp_1.Verify(x => x.InsertEvent(auditEvent),  Times.Exactly(eventType == "A" ? 1 : 0));
            dp_2.Verify(x => x.InsertEvent(auditEvent), Times.Exactly(eventType == "B" ? 1 : 0));
            dp_3.Verify(x => x.InsertEvent(auditEvent), Times.Exactly(eventType == "C" ? 1 : 0));
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public async Task Test_InsertEventAsync_WhenOtherwise(string eventType)
        {
            // Arrange
            var auditEvent = new AuditEvent() { EventType = eventType };

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var dp_3 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_3.Setup(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1.Object)
                .When(ev => ev.EventType == "B", dp_2.Object)
                .Otherwise(dp_3.Object));

            // Act
            await conditionalDataProvider.InsertEventAsync(auditEvent);

            // Assert
            dp_1.Verify(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "A" ? 1 : 0));
            dp_2.Verify(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "B" ? 1 : 0));
            dp_3.Verify(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "C" ? 1 : 0));
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public void Test_ReplaceEvent_WhenOtherwise(string eventType)
        {
            // Arrange
            var auditEvent = new AuditEvent() { EventType = eventType };

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent));

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent));

            var dp_3 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_3.Setup(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent));

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1.Object)
                .When(ev => ev.EventType == "B", dp_2.Object)
                .Otherwise(dp_3.Object));

            // Act
            conditionalDataProvider.ReplaceEvent(null, auditEvent);

            // Assert
            dp_1.Verify(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent), Times.Exactly(eventType == "A" ? 1 : 0));
            dp_2.Verify(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent), Times.Exactly(eventType == "B" ? 1 : 0));
            dp_3.Verify(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent), Times.Exactly(eventType == "C" ? 1 : 0));
        }

        
        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public async Task Test_ReplaceEventAsync_WhenOtherwise(string eventType)
        {
            // Arrange
            var auditEvent = new AuditEvent() { EventType = eventType };

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var dp_3 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_3.Setup(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>(null));

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1.Object)
                .When(ev => ev.EventType == "B", dp_2.Object)
                .Otherwise(dp_3.Object));

            // Act
            await conditionalDataProvider.ReplaceEventAsync(null, auditEvent);

            // Assert
            dp_1.Verify(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "A" ? 1 : 0));
            dp_2.Verify(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "B" ? 1 : 0));
            dp_3.Verify(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(eventType == "C" ? 1 : 0));
        }

        [Test]
        public void Test_Creation_Strategies()
        {
            // Arrange
            var dp_1_direct = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1_direct.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((object)null);

            var dp_2_factory = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2_factory.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((object)null);

            var dp_3_lazy = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_3_lazy.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((object)null);

            var dcount = 0;
            var lcount = 0;
            
            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1_direct.Object)            // Instance
                .When(ev => ev.EventType == "B", ev =>                          // Deferred
                {
                    dcount++;
                    return dp_2_factory.Object;
                })     
                .When(ev => ev.EventType == "C", () =>                          // Lazy
                {
                    lcount++;
                    return dp_3_lazy.Object;
                }));

            // Act
            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "A" });

            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "B" });
            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "B" });

            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "C" });
            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "C" });
            conditionalDataProvider.InsertEvent(new AuditEvent() { EventType = "C" });

            // Assert
            Assert.That(dcount, Is.EqualTo(2));
            Assert.That(lcount, Is.EqualTo(1));
            dp_1_direct.Verify(x => x.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
            dp_2_factory.Verify(x => x.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
            dp_3_lazy.Verify(x => x.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(3));
        }



        [Test]
        public void Test_Serialize()
        {
            // Arrange
            var auditEvent = new AuditEvent() { EventType = "B" };

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.CloneValue("test", auditEvent)).Returns((object)null);

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.CloneValue("test", auditEvent)).Returns((object)null);

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev.EventType == "A", dp_1.Object)
                .Otherwise(dp_2.Object));

            // Act
            conditionalDataProvider.CloneValue("test", auditEvent);
            conditionalDataProvider.CloneValue("test", auditEvent);

            // Assert
            dp_1.Verify(x => x.CloneValue("test", auditEvent), Times.Never);
            dp_2.Verify(x => x.CloneValue("test", auditEvent), Times.Exactly(2));
        }

        [Test]
        public void Test_GetEvent()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp_1 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_1.Setup(x => x.GetEvent<AuditEvent>("test")).Returns(auditEvent);

            var dp_2 = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp_2.Setup(x => x.GetEvent<AuditEvent>("test")).Returns(auditEvent);

            var conditionalDataProvider = new ConditionalDataProvider(config => config
                .When(ev => ev == null, dp_2.Object)
                .Otherwise(dp_1.Object));

            // Act
            conditionalDataProvider.GetEvent<AuditEvent>("test");

            // Assert
            dp_1.Verify(x => x.GetEvent<AuditEvent>("test"), Times.Never);
            dp_2.Verify(x => x.GetEvent<AuditEvent>("test"), Times.Exactly(1));
        }

        [Test]
        public async Task Test_GetEventAsync()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.GetEventAsync<AuditEvent>(1, It.IsAny<CancellationToken>())).Returns(Task.FromResult(auditEvent));

            int count = 0;

            var deferredDataProvider = new DeferredDataProvider()
            {
                Factory = ev =>
                {
                    count++;
                    return dp.Object;
                }
            };

            // Act
            await deferredDataProvider.GetEventAsync<AuditEvent>(1);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.GetEventAsync<AuditEvent>(1, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public void Test_UseConditional()
        {
            var dp1 = new InMemoryDataProvider();
            var dp2 = new InMemoryDataProvider();
            
            Audit.Core.Configuration.Setup()
                .UseConditional(c => c
                    .When(ev => ev.EventType == "A", dp1)
                    .Otherwise(dp2))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            AuditScope.Log("A", null);
            AuditScope.Log("B", null);

            Assert.That(dp1.GetAllEvents().Count, Is.EqualTo(1));
            Assert.That(dp1.GetAllEvents()[0].EventType, Is.EqualTo("A"));
            Assert.That(dp2.GetAllEvents().Count, Is.EqualTo(1));
            Assert.That(dp2.GetAllEvents()[0].EventType, Is.EqualTo("B"));
        }
    }
}
