using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class AuditDbContextAttributeTests
    {
        [Test]
        public void ExcludeTransactionId_Default_IsFalse()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.ExcludeTransactionId, Is.False);
        }

        [Test]
        public void ExcludeTransactionId_SetTrue_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ExcludeTransactionId = true;
            Assert.That(attr.ExcludeTransactionId, Is.True);
        }

        [Test]
        public void ExcludeTransactionId_SetFalse_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ExcludeTransactionId = false;
            Assert.That(attr.ExcludeTransactionId, Is.False);
        }

        [Test]
        public void IncludeEntityObjects_Default_IsFalse()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.IncludeEntityObjects, Is.False);
        }

        [Test]
        public void IncludeEntityObjects_SetTrue_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.IncludeEntityObjects = true;
            Assert.That(attr.IncludeEntityObjects, Is.True);
        }

        [Test]
        public void IncludeEntityObjects_SetFalse_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.IncludeEntityObjects = false;
            Assert.That(attr.IncludeEntityObjects, Is.False);
        }

        [Test]
        public void ExcludeValidationResults_Default_IsFalse()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.ExcludeValidationResults, Is.False);
        }

        [Test]
        public void ExcludeValidationResults_SetTrue_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ExcludeValidationResults = true;
            Assert.That(attr.ExcludeValidationResults, Is.True);
        }

        [Test]
        public void ExcludeValidationResults_SetFalse_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ExcludeValidationResults = false;
            Assert.That(attr.ExcludeValidationResults, Is.False);
        }

        [Test]
        public void Mode_Default_IsOptOut()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.Mode, Is.EqualTo(AuditOptionMode.OptOut));
        }

        [Test]
        public void Mode_SetOptIn_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.Mode = AuditOptionMode.OptIn;
            Assert.That(attr.Mode, Is.EqualTo(AuditOptionMode.OptIn));
        }

        [Test]
        public void Mode_SetOptOut_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.Mode = AuditOptionMode.OptOut;
            Assert.That(attr.Mode, Is.EqualTo(AuditOptionMode.OptOut));
        }

        [Test]
        public void AuditEventType_Default_IsNull()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.AuditEventType, Is.Null);
        }

        [Test]
        public void AuditEventType_SetValue_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.AuditEventType = "MyEventType";
            Assert.That(attr.AuditEventType, Is.EqualTo("MyEventType"));
        }

        [Test]
        public void ReloadDatabaseValues_Default_IsFalse()
        {
            var attr = new AuditDbContextAttribute();
            Assert.That(attr.ReloadDatabaseValues, Is.False);
        }

        [Test]
        public void ReloadDatabaseValues_SetTrue_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ReloadDatabaseValues = true;
            Assert.That(attr.ReloadDatabaseValues, Is.True);
        }

        [Test]
        public void ReloadDatabaseValues_SetFalse_ReflectsValue()
        {
            var attr = new AuditDbContextAttribute();
            attr.ReloadDatabaseValues = false;
            Assert.That(attr.ReloadDatabaseValues, Is.False);
        }
    }
}