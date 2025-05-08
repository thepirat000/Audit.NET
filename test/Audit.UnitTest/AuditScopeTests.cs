using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public void Test_ScopeItems()
        {
            var testObject = new object();

            object onCreatedObject = null;

            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

            Audit.Core.Configuration.AddOnCreatedAction(scope =>
            {
                onCreatedObject = scope.Items["TestObject"];
            });

            var scope = AuditScope.Create(c => c.EventType("Event").WithItem("TestObject", testObject));

            var afterCreatedObject = scope.Items["TestObject"];

            scope.Dispose();

            var evs = dp.GetAllEvents();

            Assert.That(evs, Has.Count.EqualTo(1));
            Assert.That(onCreatedObject, Is.Not.Null);
            Assert.That(onCreatedObject, Is.EqualTo(testObject));
            Assert.That(afterCreatedObject, Is.Not.Null);
            Assert.That(afterCreatedObject, Is.EqualTo(testObject));
        }

        [Test]
        public void Test_ScopeItems_GetItem()
        {
            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);
            
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                Items = new() { { "Key", new ArgumentException("test") }}
            });

            var getItemAsObject = scope.GetItem<object>("Key");
            var getItemAsException = scope.GetItem<Exception>("Key");
            var getItemAsArgumentException = scope.GetItem<ArgumentException>("Key");
            
            var getItemDifferentType = scope.GetItem<OutOfMemoryException>("Key");
            var getItemKeyNotFound = scope.GetItem<object>("NotFound");

            scope.Dispose();

            Assert.That(getItemAsObject, Is.Not.Null);
            Assert.That(getItemAsException, Is.Not.Null);
            Assert.That(getItemAsArgumentException, Is.Not.Null);
            
            Assert.That(getItemDifferentType, Is.Null);
            Assert.That(getItemKeyNotFound, Is.Null);
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
        private void DoScope(AuditEvent auditEvent, IAuditDataProvider dp)
        {
            using var scope = AuditScope.Create(c => c
                .AuditEvent(auditEvent)
                .CreationPolicy(EventCreationPolicy.InsertOnEnd)
                .DataProvider(dp));
            scope.SetCustomField("Test", 1);

            Assert.That(auditEvent.GetScope(), Is.Not.Null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task DoScopeAsync(AuditEvent auditEvent, IAuditDataProvider dp)
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
