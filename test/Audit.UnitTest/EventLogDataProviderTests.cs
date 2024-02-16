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
            Assert.That(x.LogName.GetDefault(), Is.EqualTo("LogName"));
            Assert.That(x.MachineName.GetDefault(), Is.EqualTo("MachineName"));
            Assert.That(x.MessageBuilder.Invoke(new AuditEvent()), Is.EqualTo("MessageBuilder"));
            Assert.That(x.SourcePath.GetDefault(), Is.EqualTo("SourcePath"));
        }

        [Test]
        public void Test_EventLogDataProvider_FluentApi_Builder()
        {
            var x = new EventLogDataProvider(_ => _
                .LogName(ev => "LogName")
                .MachineName(ev => "MachineName")
                .MessageBuilder(ev => "MessageBuilder")
                .SourcePath(ev => "SourcePath")
            );
            Assert.That(x.LogName.GetValue(new AuditEvent()), Is.EqualTo("LogName"));
            Assert.That(x.MachineName.GetValue(new AuditEvent()), Is.EqualTo("MachineName"));
            Assert.That(x.MessageBuilder.Invoke(new AuditEvent()), Is.EqualTo("MessageBuilder"));
            Assert.That(x.SourcePath.GetValue(new AuditEvent()), Is.EqualTo("SourcePath"));
        }
    }
}
