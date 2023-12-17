using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    public class InMemoryDataProviderTests
    {
        public class CustomAuditEvent : AuditEvent
        {
            public int AuditEventId { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        public void Test_InMemoryDataProvider(EventCreationPolicy creationPolicy)
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithCreationPolicy(creationPolicy);

            var ev = new CustomAuditEvent() { AuditEventId = 123 };
            var target = new List<string>() { "initial" };
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "test", TargetGetter = () => target, AuditEvent = ev }))
            {
                target.Add("final");
            }

            var dp = Configuration.DataProviderAs<InMemoryDataProvider>();
            var event0 = dp.GetEvent(0);
            var allEvents = dp.GetAllEvents();
            var allCustomEvents = dp.GetAllEventsOfType<CustomAuditEvent>();

            Assert.That(event0, Is.Not.Null);
            Assert.That(allEvents, Is.Not.Null);
            Assert.That(allCustomEvents, Is.Not.Null);
            Assert.That(allEvents.Count, Is.EqualTo(1));
            Assert.That(allCustomEvents.Count, Is.EqualTo(1));

            var old = event0.Target.Old as List<string>;
            var @new = event0.Target.New as List<string>;

            Assert.That(old, Is.Not.Null);
            Assert.That(@new, Is.Not.Null);
            Assert.That(old.Count, Is.EqualTo(1));
            Assert.That(@new.Count, Is.EqualTo(2));
            Assert.That(old[0], Is.EqualTo("initial"));
            Assert.That(@new[0], Is.EqualTo("initial"));
            Assert.That(@new[1], Is.EqualTo("final"));
            Assert.That(event0.EventType, Is.EqualTo("test"));
            Assert.That(allEvents[0].EventType, Is.EqualTo("test"));
            Assert.That(allCustomEvents[0].EventType, Is.EqualTo("test"));
            Assert.That(allCustomEvents[0].AuditEventId, Is.EqualTo(123));

            dp.ClearEvents();

            var allEventsAfterClear = dp.GetAllEvents();
            Assert.That(allEventsAfterClear.Count, Is.EqualTo(0));
        }

        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        public async Task Test_InMemoryDataProviderAsync(EventCreationPolicy creationPolicy)
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithCreationPolicy(creationPolicy);

            var ev = new CustomAuditEvent() { AuditEventId = 123 };
            var target = new List<string>() { "initial" };
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "test", TargetGetter = () => target, AuditEvent = ev }))
            {
                target.Add("final");
            }

            var dp = Configuration.DataProviderAs<InMemoryDataProvider>();
            var event0 = await dp.GetEventAsync(0);
            var allEvents = dp.GetAllEvents();
            var allCustomEvents = dp.GetAllEventsOfType<CustomAuditEvent>();

            Assert.That(event0, Is.Not.Null);
            Assert.That(allEvents, Is.Not.Null);
            Assert.That(allCustomEvents, Is.Not.Null);
            Assert.That(allEvents.Count, Is.EqualTo(1));
            Assert.That(allCustomEvents.Count, Is.EqualTo(1));

            var old = event0.Target.Old as List<string>;
            var @new = event0.Target.New as List<string>;

            Assert.That(old, Is.Not.Null);
            Assert.That(@new, Is.Not.Null);
            Assert.That(old.Count, Is.EqualTo(1));
            Assert.That(@new.Count, Is.EqualTo(2));
            Assert.That(old[0], Is.EqualTo("initial"));
            Assert.That(@new[0], Is.EqualTo("initial"));
            Assert.That(@new[1], Is.EqualTo("final"));
            Assert.That(event0.EventType, Is.EqualTo("test"));
            Assert.That(allEvents[0].EventType, Is.EqualTo("test"));
            Assert.That(allCustomEvents[0].EventType, Is.EqualTo("test"));
            Assert.That(allCustomEvents[0].AuditEventId, Is.EqualTo(123));

            dp.ClearEvents();

            var allEventsAfterClear = dp.GetAllEvents();
            Assert.That(allEventsAfterClear.Count, Is.EqualTo(0));
        }
    }
}