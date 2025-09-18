using Audit.Core;
using Audit.Core.Providers.Wrappers;

using NUnit.Framework;

using System.Threading.Tasks;

namespace Audit.UnitTest
{
    [TestFixture]
    public class WrapperDataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void WrapperDataProvider_GetDataProviderNull_DoesNothing()
        {
            var dp = new WrapperDataProviderNull();
            var ev = new AuditEvent();
            
            var insertEvent = dp.InsertEvent(ev);
            dp.ReplaceEvent(null, ev);
            var clone = dp.CloneValue("test", ev);
            var getEvent = dp.GetEvent<AuditEvent>(null);

            Assert.That(insertEvent, Is.Null);
            Assert.That(clone, Is.EqualTo("test"));
            Assert.That(getEvent, Is.Null);
        }

        [Test]
        public async Task WrapperDataProvider_GetDataProviderNull_DoesNothingAsync()
        {
            var dp = new WrapperDataProviderNull();
            var ev = new AuditEvent();

            var insertEvent = await dp.InsertEventAsync(ev);
            await dp.ReplaceEventAsync(null, ev);
            var getEvent = dp.GetEventAsync<AuditEvent>(null);

            Assert.That(insertEvent, Is.Null);
            Assert.That(getEvent, Is.Null);
        }

        private class WrapperDataProviderNull : WrapperDataProvider
        {
            protected override IAuditDataProvider GetDataProvider(AuditEvent auditEvent)
            {
                return null;
            }
        }
    }

}
