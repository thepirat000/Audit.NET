using System;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("EventLog")]
    public class EventLogDataProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            Core.Configuration.Reset();
        }

        [Test]
        public void TestEventLogDataProvider_InsertReplaceEvent()
        {
            // Arrange
            var dataProvider = new EventLogDataProvider()
            {
                LogName = "Application",
                SourcePath = "Application",
                MessageBuilder = ev => ev.EventType
            };

            var auditEventException = new AuditEvent()
            {
                EventType = $"test {Guid.NewGuid()}",
                Environment = new AuditEventEnvironment() { Exception = "SomeException" }
            };
            var auditEventOk = new AuditEvent()
            {
                EventType = $"test {Guid.NewGuid()}", 
                Environment = new AuditEventEnvironment() { Exception = null }
            };

            // Act
            var eventId1 = dataProvider.InsertEvent(auditEventException);
            dataProvider.ReplaceEvent(null, auditEventOk);

            // Assert
            Assert.That(eventId1, Is.Null);
        }

        [Test]
        public void Test_EventLogDataProvider_FluentApi()
        {
            var x = new EventLogDataProvider(_ => _
                .LogName("LogName")
                .MachineName("MachineName")
                .MessageBuilder(ev => "MessageBuilder")
                .SourcePath("SourcePath")
            );
            Assert.That(x.LogName, Is.EqualTo("LogName"));
            Assert.That(x.MachineName, Is.EqualTo("MachineName"));
            Assert.That(x.MessageBuilder.Invoke(new AuditEvent()), Is.EqualTo("MessageBuilder"));
            Assert.That(x.SourcePath, Is.EqualTo("SourcePath"));
        }
    }
}
