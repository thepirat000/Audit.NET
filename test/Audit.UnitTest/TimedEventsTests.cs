using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.UnitTest
{
    [TestFixture]
    public class TimedEventsTests
    {
        [SetUp]
        public void Setup()
        {
            Configuration.Reset();
        }

        [Test]
        public void Test_TimedEvent_Serialization()
        {
            var timedEvent = new Audit.Core.TimedEvent
            {
                Date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Timestamp = 1234567890,
                Offset = 100,
                Data = new { Message = "Test data" },
                CustomFields = new Dictionary<string, object>
                {
                    { "CustomField1", "Value1" },
                    { "CustomField2", 42 }
                }
            };
            var json = timedEvent.ToJson();
            Console.WriteLine(json);
            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("\"Date\":\"2024-01-01T12:00:00Z\""));
                Assert.That(json, Does.Contain("\"Timestamp\":1234567890"));
                Assert.That(json, Does.Contain("\"Offset\":100"));
                Assert.That(json, Does.Contain("\"Data\":{\"Message\":\"Test data\"}"));
                Assert.That(json, Does.Contain("\"CustomField1\":\"Value1\""));
                Assert.That(json, Does.Contain("\"CustomField2\":42"));
            });
        }

        [Test]
        public async Task Test_TimedEvent_EmptyCustomFields_Serialization()
        {
            var dp = new InMemoryDataProvider();
            
            Configuration.IncludeTimestamps = true;

            var auditScope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions()
            {
                EventType = "Test",
                DataProvider = dp
            });

            auditScope.AddTimedEvent("Test String 1");
            await Task.Delay(2);
            auditScope.AddTimedEvent("Test String 2", new Dictionary<string, object>() { ["#"] = 10 } );
            await Task.Delay(2);
            auditScope.AddTimedEvent(new { Object = "Test Object 1" });
            await Task.Delay(2);
            auditScope.AddTimedEvent(new { Object = "Test Object 1" }, new Dictionary<string, object>() { ["#"] = 20 } );
            await Task.Delay(2);
            auditScope.AddTimedEvent(null);
            await Task.Delay(2);
            auditScope.AddTimedEvent(null, new Dictionary<string, object>() { ["#"] = 20 } );

            await Task.Delay(2);
            auditScope.AddTimedEvent(new { Object = "Test Object 3" }, new { Property = 30 });


            await auditScope.DisposeAsync();

            var auditEvent = dp.GetAllEvents()[0];

            Assert.Multiple(() =>
            {
                Assert.That(auditEvent, Is.Not.Null);
                Assert.That(auditEvent.TimedEvents, Has.Count.EqualTo(7));
                Assert.That(auditEvent.TimedEvents[0].Data, Is.EqualTo("Test String 1"));
                Assert.That(auditEvent.TimedEvents[0].CustomFields, Is.Null);
                Assert.That(auditEvent.TimedEvents[1].Data, Is.EqualTo("Test String 2"));
                Assert.That(auditEvent.TimedEvents[1].CustomFields, Is.Not.Null);
                Assert.That(auditEvent.TimedEvents[1].CustomFields.ContainsKey("#"), Is.True);
                Assert.That(auditEvent.TimedEvents[1].CustomFields["#"].ToString(), Is.EqualTo("10"));
                Assert.That(auditEvent.TimedEvents[2].CustomFields, Is.Null);
#if !NET462 && !NET472
                Assert.That(((dynamic)auditEvent.TimedEvents[2].Data).Object, Is.EqualTo("Test Object 1"));
                Assert.That(((dynamic)auditEvent.TimedEvents[3].Data).Object, Is.EqualTo("Test Object 1"));
#endif
                Assert.That(auditEvent.TimedEvents[3].CustomFields, Is.Not.Null);
                Assert.That(auditEvent.TimedEvents[3].CustomFields.ContainsKey("#"), Is.True);
                Assert.That(auditEvent.TimedEvents[3].CustomFields["#"].ToString(), Is.EqualTo("20"));
                Assert.That(auditEvent.TimedEvents[4].Data, Is.Null);
                Assert.That(auditEvent.TimedEvents[4].CustomFields, Is.Null);
                Assert.That(auditEvent.TimedEvents[5].Data, Is.Null);
                Assert.That(auditEvent.TimedEvents[5].CustomFields, Is.Not.Null);
                Assert.That(auditEvent.TimedEvents[5].CustomFields.ContainsKey("#"), Is.True);
                Assert.That(auditEvent.TimedEvents[5].CustomFields["#"].ToString(), Is.EqualTo("20"));
                Assert.That(auditEvent.TimedEvents[0].Offset, Is.GreaterThan(1));
                Assert.That(auditEvent.TimedEvents[1].Offset, Is.GreaterThan(auditEvent.TimedEvents[0].Offset));
                Assert.That(auditEvent.TimedEvents[2].Offset, Is.GreaterThan(auditEvent.TimedEvents[1].Offset));
                Assert.That(auditEvent.TimedEvents[3].Offset, Is.GreaterThan(auditEvent.TimedEvents[2].Offset));
                Assert.That(auditEvent.TimedEvents[4].Offset, Is.GreaterThan(auditEvent.TimedEvents[3].Offset));
                Assert.That(auditEvent.TimedEvents[5].Offset, Is.GreaterThan(auditEvent.TimedEvents[4].Offset));
                Assert.That(auditEvent.TimedEvents[6].Offset, Is.GreaterThan(auditEvent.TimedEvents[5].Offset));
                Assert.That(auditEvent.TimedEvents[6].CustomFields, Is.Not.Null);
                Assert.That(auditEvent.TimedEvents[6].CustomFields, Has.Count.EqualTo(1));
                Assert.That(auditEvent.TimedEvents[6].CustomFields, Does.ContainKey("Property"));
                Assert.That(auditEvent.TimedEvents[6].CustomFields["Property"].ToString(), Is.EqualTo("30"));
                Assert.That(auditEvent.TimedEvents[6].Offset, Is.LessThanOrEqualTo(auditEvent.Duration));
                Assert.That(auditEvent.TimedEvents[0].Timestamp, Is.GreaterThan(auditEvent.StartTimestamp));
                Assert.That(auditEvent.TimedEvents[1].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[0].Timestamp));
                Assert.That(auditEvent.TimedEvents[2].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[1].Timestamp));
                Assert.That(auditEvent.TimedEvents[3].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[2].Timestamp));
                Assert.That(auditEvent.TimedEvents[4].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[3].Timestamp));
                Assert.That(auditEvent.TimedEvents[5].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[4].Timestamp));
                Assert.That(auditEvent.TimedEvents[6].Timestamp, Is.GreaterThan(auditEvent.TimedEvents[5].Timestamp));
                Assert.That(auditEvent.TimedEvents[6].Timestamp, Is.LessThanOrEqualTo(auditEvent.EndTimestamp));
            });
        }
    }
}
