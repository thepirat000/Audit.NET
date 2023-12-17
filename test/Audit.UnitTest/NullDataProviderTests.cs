using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class NullDataProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            Core.Configuration.Reset();
        }

        [Test]
        public void TestNullDataProvider_NoAction()
        {
            // Arrange
            var dataProvider = new NullDataProvider();
            var ev = new AuditEvent();

            // Act
            dataProvider.InsertEvent(ev);
            dataProvider.ReplaceEvent(1, ev);
            var evRead = dataProvider.GetEvent<AuditEvent>(1);

            // Assert
            Assert.That(evRead, Is.Null);
        }

        [Test]
        public async Task TestNullDataProvider_NoActionAsync()
        {
            // Arrange
            var dataProvider = new NullDataProvider();
            var ev = new AuditEvent() { EventType = "test1" };

            // Act
            await dataProvider.InsertEventAsync(ev);
            await dataProvider.ReplaceEventAsync(1, ev);
            var evRead = await dataProvider.GetEventAsync<AuditEvent>(1);

            // Assert
            Assert.That(evRead, Is.Null);
        }
    }
}
