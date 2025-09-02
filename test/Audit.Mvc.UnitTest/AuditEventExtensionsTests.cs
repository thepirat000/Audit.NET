using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

using System.Collections.Generic;

namespace Audit.Mvc.UnitTest
{
    [TestFixture]
    public class AuditEventExtensionsTests
    {
        [Test]
        public void GetMvcAuditAction_FromScope_ReturnsAction()
        {
            var action = new AuditAction { HttpMethod = "GET" };
            var mvcEvent = new AuditEventMvcAction { Action = action };
            using var scope = AuditScope.Create(new AuditScopeOptions { AuditEvent = mvcEvent, DataProvider = new NullDataProvider() });

            var result = scope.GetMvcAuditAction();

            Assert.That(result, Is.EqualTo(action));
        }

        [Test]
        public void GetMvcAuditAction_FromScope_NullScope_ReturnsNull()
        {
            AuditScope scope = null;
            var result = scope.GetMvcAuditAction();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetMvcAuditAction_FromAuditEvent_MvcType_ReturnsAction()
        {
            var action = new AuditAction { HttpMethod = "POST" };
            var mvcEvent = new AuditEventMvcAction { Action = action };

            var result = mvcEvent.GetMvcAuditAction();

            Assert.That(result, Is.EqualTo(action));
        }

        [Test]
        public void GetMvcAuditAction_FromAuditEvent_CustomFields_ReturnsDeserialized()
        {
            var action = new AuditAction { HttpMethod = "PUT" };
            var serialized = System.Text.Json.JsonSerializer.SerializeToElement(action);
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>
                {
                    { "Action", serialized }
                }
            };

            var result = auditEvent.GetMvcAuditAction();

            Assert.That(result.HttpMethod, Is.EqualTo("PUT"));
        }

        [Test]
        public void GetMvcAuditAction_FromAuditEvent_CustomFields_NoKey_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>()
            };

            var result = auditEvent.GetMvcAuditAction();

            Assert.That(result, Is.Null);
        }
    }
}
