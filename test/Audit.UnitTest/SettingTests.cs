using Audit.Core;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class SettingTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Test_GetValueFromBuilder(bool value)
        {
            // Arrange and act
            var setting = new Setting<bool>(ev => value);

            // Assert
            Assert.That(setting.GetValue(null), Is.EqualTo(value));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_GetValueFromValue(bool value)
        {
            // Arrange and act
            var setting = new Setting<bool>(value);

            // Assert
            Assert.That(setting.GetValue(null), Is.EqualTo(value));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_GetValueImplicitValue(bool value)
        {
            // Arrange and act
            Setting<bool> setting = value;

            // Assert
            Assert.That(setting.GetValue(null), Is.EqualTo(value));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_GetValueFromBuilder_WithAuditEvent(bool value)
        {
            // Arrange
            var eventType = nameof(Test_GetValueFromBuilder_WithAuditEvent);
            var auditEvent = new AuditEvent() { EventType = eventType };
            
            // Act && Assert
            var setting = new Setting<bool>(ev =>
            {
                Assert.That(ev.EventType, Is.EqualTo(eventType));
                return value;
            });

            // Assert
            Assert.That(setting.GetValue(auditEvent), Is.EqualTo(value));
        }

        [Test]
        public void Test_GetValue_Calls_The_Builder_Function()
        {
            // Arrange
            var eventType = nameof(Test_GetValue_Calls_The_Builder_Function);
            var auditEvent = new AuditEvent() { EventType = eventType };
            var calls = 0;
            var setting = new Setting<bool>(ev =>
            {
                calls++;
                return true;
            });

            // Act
            setting.GetValue(auditEvent);
            setting.GetValue(auditEvent);

            // Assert
            Assert.That(calls, Is.EqualTo(2));
        }
    }
}
