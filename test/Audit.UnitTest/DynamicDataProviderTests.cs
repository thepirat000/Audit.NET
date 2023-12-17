using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class DynamicDataProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            Core.Configuration.Reset();
        }

        [Test]
        public void TestDynamicDataProvider_OnInsert()
        {
            // Arrange
            var events = new List<AuditEvent>();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.Setup()
                .UseDynamicAsyncProvider(c => c.OnInsert(async (ev, ct) =>
                {
                    events.Add(ev);
                    await Task.Delay(0, ct);
                }));

            var scope = AuditScope.Create("test", null);
            scope.Save();

            // Assert
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestDynamicDataProvider_OnInsertAndReplace()
        {
            // Arrange
            var events = new List<AuditEvent>();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.Setup()
                .UseDynamicAsyncProvider(c => c.OnInsertAndReplace(async (ev, ct) =>
                {
                    events.Add(ev);
                    await Task.Delay(0, ct);
                }).OnInsertAndReplace(async ev =>
                {
                    events.Add(ev);
                    await Task.Delay(0);
                }).OnInsertAndReplace(async (eventId, ev) =>
                {
                    events.Add(ev);
                    await Task.Delay(0);
                }).OnInsertAndReplace(async (eventId, ev, ct) =>
                {
                    events.Add(ev);
                    await Task.Delay(0, ct);
                }));

            var scope = AuditScope.Create("test", null);
            scope.Save();

            // Assert
            Assert.That(events.Count, Is.EqualTo(4));
        }
    }
}
