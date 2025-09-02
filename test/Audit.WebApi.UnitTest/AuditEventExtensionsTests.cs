using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

using System.Collections.Generic;

namespace Audit.WebApi.UnitTest
{
    [TestFixture]
    public class AuditEventExtensionsTests
    {
        [Test]
        public void GetWebApiAuditAction_FromScope_ReturnsAction()
        {
            var action = new AuditApiAction { HttpMethod = "GET" };
            var apiEvent = new AuditEventWebApi { Action = action };
            using var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = apiEvent, DataProvider = new NullDataProvider() });

            var result = scope.GetWebApiAuditAction();

            Assert.That(result, Is.EqualTo(action));
        }

        [Test]
        public void GetWebApiAuditAction_FromScope_NullScope_ReturnsNull()
        {
            AuditScope scope = null;
            var result = scope.GetWebApiAuditAction();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetWebApiAuditAction_FromAuditEvent_WebApiType_ReturnsAction()
        {
            var action = new AuditApiAction { HttpMethod = "POST" };
            var apiEvent = new AuditEventWebApi { Action = action };

            var result = apiEvent.GetWebApiAuditAction();

            Assert.That(result, Is.EqualTo(action));
        }

        [Test]
        public void GetWebApiAuditAction_FromAuditEvent_CustomFields_ReturnsDeserialized()
        {
            var action = new AuditApiAction { HttpMethod = "PUT" };
            var serialized = System.Text.Json.JsonSerializer.SerializeToElement(action);
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>
                {
                    { "Action", serialized }
                }
            };

            var result = auditEvent.GetWebApiAuditAction();

            Assert.That(result.HttpMethod, Is.EqualTo("PUT"));
        }

        [Test]
        public void GetWebApiAuditAction_FromAuditEvent_CustomFields_NoKey_ReturnsNull()
        {
            var auditEvent = new AuditEvent
            {
                CustomFields = new Dictionary<string, object>()
            };

            var result = auditEvent.GetWebApiAuditAction();

            Assert.That(result, Is.Null);
        }
    }
}
