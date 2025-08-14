using System.Collections.Generic;

using Audit.Core;

using Moq;

using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class AuditEventExtensionsTests
    {
        [Test]
        public void GetEntityFrameworkEvent_FromScope_ReturnsEvent()
        {
            var efEvent = new EntityFrameworkEvent { Database = "TestDb" };
            var auditEvent = new AuditEventEntityFramework { EntityFrameworkEvent = efEvent };
            var scopeMock = new Mock<IAuditScope>();
            scopeMock.Setup(s => s.Event).Returns(auditEvent);

            var result = scopeMock.Object.GetEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(efEvent));
        }

        [Test]
        public void GetEntityFrameworkEvent_FromScope_NullScope_ReturnsNull()
        {
            IAuditScope scope = null;
            var result = scope.GetEntityFrameworkEvent();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEntityFrameworkEvent_FromAuditEvent_EntityFrameworkType_ReturnsEvent()
        {
            var efEvent = new EntityFrameworkEvent { Database = "TestDb" };
            var auditEvent = new AuditEventEntityFramework { EntityFrameworkEvent = efEvent };

            var result = auditEvent.GetEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(efEvent));
        }

        [Test]
        public void GetEntityFrameworkEvent_FromAuditEvent_CustomFields_ReturnsDeserialized()
        {
            var efEvent = new EntityFrameworkEvent { Database = "TestDb" };
            var serialized = System.Text.Json.JsonSerializer.SerializeToElement(efEvent);
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>
                {
                    { "EntityFrameworkEvent", serialized }
                }
            };

            var result = auditEvent.GetEntityFrameworkEvent();

            Assert.That(result.Database, Is.EqualTo("TestDb"));
        }

        [Test]
        public void GetEntityFrameworkEvent_FromAuditEvent_CustomFields_NoKey_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>()
            };

            var result = auditEvent.GetEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEntityFrameworkEvent_FromAuditEvent_NullCustomFields_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = []
            };

            var result = auditEvent.GetEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }

#if EF_CORE_5_OR_GREATER
        [Test]
        public void GetCommandEntityFrameworkEvent_FromScope_ReturnsEvent()
        {
            var cmdEvent = new CommandEvent { CommandText = "SELECT 1" };
            var auditEvent = new AuditEventCommandEntityFramework { CommandEvent = cmdEvent };
            var scopeMock = new Mock<IAuditScope>();
            scopeMock.Setup(s => s.Event).Returns(auditEvent);

            var result = scopeMock.Object.GetCommandEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(cmdEvent));
        }

        [Test]
        public void GetCommandEntityFrameworkEvent_FromScope_NullScope_ReturnsNull()
        {
            IAuditScope scope = null;
            var result = scope.GetCommandEntityFrameworkEvent();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetCommandEntityFrameworkEvent_FromAuditEvent_CommandType_ReturnsEvent()
        {
            var cmdEvent = new CommandEvent { CommandText = "SELECT 1" };
            var auditEvent = new AuditEventCommandEntityFramework { CommandEvent = cmdEvent };

            var result = auditEvent.GetCommandEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(cmdEvent));
        }

        [Test]
        public void GetCommandEntityFrameworkEvent_FromAuditEvent_CustomFields_ReturnsDeserialized()
        {
            var cmdEvent = new CommandEvent { CommandText = "SELECT 1" };
            var serialized = System.Text.Json.JsonSerializer.SerializeToElement(cmdEvent);
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>
                {
                    { "CommandEvent", serialized }
                }
            };

            var result = auditEvent.GetCommandEntityFrameworkEvent();

            Assert.That(result.CommandText, Is.EqualTo("SELECT 1"));
        }

        [Test]
        public void GetCommandEntityFrameworkEvent_FromAuditEvent_CustomFields_NoKey_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>()
            };

            var result = auditEvent.GetCommandEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetCommandEntityFrameworkEvent_FromAuditEvent_NullCustomFields_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = []
            };

            var result = auditEvent.GetCommandEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromScope_ReturnsEvent()
        {
            var tranEvent = new TransactionEvent { Action = "Commit" };
            var auditEvent = new AuditEventTransactionEntityFramework { TransactionEvent = tranEvent };
            var scopeMock = new Mock<IAuditScope>();
            scopeMock.Setup(s => s.Event).Returns(auditEvent);

            var result = scopeMock.Object.GetTransactionEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(tranEvent));
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromScope_NullScope_ReturnsNull()
        {
            IAuditScope scope = null;
            var result = scope.GetTransactionEntityFrameworkEvent();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromAuditEvent_TransactionType_ReturnsEvent()
        {
            var tranEvent = new TransactionEvent { Action = "Commit" };
            var auditEvent = new AuditEventTransactionEntityFramework { TransactionEvent = tranEvent };

            var result = auditEvent.GetTransactionEntityFrameworkEvent();

            Assert.That(result, Is.EqualTo(tranEvent));
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromAuditEvent_CustomFields_ReturnsDeserialized()
        {
            var tranEvent = new TransactionEvent { Action = "Commit" };
            var serialized = System.Text.Json.JsonSerializer.SerializeToElement(tranEvent);
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>
                {
                    { "TransactionEvent", serialized }
                }
            };

            var result = auditEvent.GetTransactionEntityFrameworkEvent();

            Assert.That(result.Action, Is.EqualTo("Commit"));
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromAuditEvent_CustomFields_NoKey_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>()
            };

            var result = auditEvent.GetTransactionEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetTransactionEntityFrameworkEvent_FromAuditEvent_NullCustomFields_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = []
            };

            var result = auditEvent.GetTransactionEntityFrameworkEvent();

            Assert.That(result, Is.Null);
        }
#endif
    }
}