using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class AuditScopeTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }
        
        [Test]
        public void Test_AuditEvent_GetAuditScope_WeakReference()
        {
            // Arrange
            var auditEvent = new AuditEvent()
            {
                EventType = "TestScope"
            };

            // Act
            var dp = new InMemoryDataProvider();

            DoScope(auditEvent, dp);

#pragma warning disable S1215
            GC.Collect();
#pragma warning restore S1215

            var scopeAfterCollect = auditEvent.GetScope();

            // Assert
            Assert.That(scopeAfterCollect, Is.Null);
            Assert.That(dp.GetAllEvents().Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_AuditEvent_GetAuditScope_WeakReferenceAsync()
        {
            // Arrange
            var auditEvent = new AuditEvent()
            {
                EventType = "TestScope"
            };

            // Act
            var dp = new InMemoryDataProvider();

            await DoScopeAsync(auditEvent, dp);

#pragma warning disable S1215
            GC.Collect();
#pragma warning restore S1215

            var scopeAfterCollect = auditEvent.GetScope();

            // Assert
            Assert.That(scopeAfterCollect, Is.Null);
            Assert.That(dp.GetAllEvents().Count, Is.EqualTo(1));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DoScope(AuditEvent auditEvent, AuditDataProvider dp)
        {
            using var scope = AuditScope.Create(c => c
                .AuditEvent(auditEvent)
                .CreationPolicy(EventCreationPolicy.InsertOnEnd)
                .DataProvider(dp));
            scope.SetCustomField("Test", 1);

            Assert.That(auditEvent.GetScope(), Is.Not.Null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task DoScopeAsync(AuditEvent auditEvent, AuditDataProvider dp)
        {
            using var scope = await AuditScope.CreateAsync(c => c
                .AuditEvent(auditEvent)
                .CreationPolicy(EventCreationPolicy.InsertOnEnd)
                .DataProvider(dp));

            scope.SetCustomField("Test", 1);

            Assert.That(auditEvent.GetScope(), Is.Not.Null);
        }
    }
}
