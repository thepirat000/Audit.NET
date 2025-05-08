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
    public class LazyDataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_InsertReplaceEvent()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.InsertEvent(auditEvent)).Returns((object)null);
            dp.Setup(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent));
            
            int count = 0;

            var lazyDataProvider = new LazyDataProvider(() =>
            {
                count++;
                return dp.Object;
            });

            // Act
            lazyDataProvider.InsertEvent(auditEvent);
            lazyDataProvider.ReplaceEvent(null, auditEvent);
            lazyDataProvider.InsertEvent(auditEvent);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.InsertEvent(auditEvent), Times.Exactly(2));
            dp.Verify(x => x.ReplaceEvent(It.IsAny<object>(), auditEvent), Times.Exactly(1));
        }
        

        [Test]
        public async Task Test_InsertReplaceEventAsync()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>())).ReturnsAsync((object)null);
            dp.Setup(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            int count = 0;

            var lazyDataProvider = new LazyDataProvider()
            {
                Factory = () =>
                {
                    count++;
                    return dp.Object;
                }
            };

            // Act
            await lazyDataProvider.InsertEventAsync(auditEvent, default);
            await lazyDataProvider.ReplaceEventAsync(null, auditEvent, default);
            await lazyDataProvider.InsertEventAsync(auditEvent, default);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.InsertEventAsync(auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(2));
            dp.Verify(x => x.ReplaceEventAsync(It.IsAny<object>(), auditEvent, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public void Test_Serialize()
        {
            // Arrange
            var auditEvent = new AuditEvent();
            
            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.CloneValue("test", auditEvent)).Returns((object)null);

            int count = 0;

            var lazyDataProvider = new LazyDataProvider(() =>
            {
                count++;
                return dp.Object;
            });

            // Act
            lazyDataProvider.CloneValue("test", auditEvent);
            lazyDataProvider.CloneValue("test", auditEvent);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.CloneValue("test", auditEvent), Times.Exactly(2));
        }

        [Test]
        public void Test_GetEvent()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.GetEvent<AuditEvent>(1)).Returns(auditEvent);

            int count = 0;

            var lazyDataProvider = new LazyDataProvider(() =>
            {
                count++;
                return dp.Object;
            });

            // Act
            lazyDataProvider.GetEvent<AuditEvent>(1);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.GetEvent<AuditEvent>(1), Times.Exactly(1));
        }

        [Test]
        public async Task Test_GetEventAsync()
        {
            // Arrange
            var auditEvent = new AuditEvent();

            var dp = new Mock<IAuditDataProvider>(MockBehavior.Strict);
            dp.Setup(x => x.GetEventAsync<AuditEvent>(1, It.IsAny<CancellationToken>())).Returns(Task.FromResult(auditEvent));

            int count = 0;

            var lazyDataProvider = new LazyDataProvider(() =>
            {
                count++;
                return dp.Object;
            });

            // Act
            await lazyDataProvider.GetEventAsync<AuditEvent>(1);

            // Assert
            Assert.That(count, Is.EqualTo(1));
            dp.Verify(x => x.GetEventAsync<AuditEvent>(1, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public void Test_UseLazy()
        {
            var dp1 = new InMemoryDataProvider();

            Audit.Core.Configuration.Setup()
                .UseLazyFactory(() => dp1)
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            AuditScope.Log("A", null);
            AuditScope.Log("B", null);

            Assert.That(dp1.GetAllEvents().Count, Is.EqualTo(2));
        }
    }
}