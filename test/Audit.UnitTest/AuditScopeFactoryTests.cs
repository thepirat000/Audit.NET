using System.Threading.Tasks;

using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class AuditScopeFactoryTests
    {
        [SetUp]
        public void SetUp()
        {
            Configuration.Reset();
            Configuration.DataProvider = new InMemoryDataProvider();
            Configuration.CreationPolicy = EventCreationPolicy.Manual;
        }

        [Test]
        public void Test_Create_OnScopeCreated_OnConfiguring_Calls()
        {
            // Arrange
            var auditScopeFactory = new MockAuditScopeFactory();
            
            // Act
            var scope = auditScopeFactory.Create(new AuditScopeOptions());
            scope.Save();
            scope.Dispose();

            scope = auditScopeFactory.Create(cfg => cfg.EventType(""));
            scope.Save();
            scope.Dispose();

            scope = auditScopeFactory.Create(eventType: "eventType", target: null);
            scope.Save();
            scope.Dispose();

            scope = auditScopeFactory.Create(eventType: "eventType", target: null, EventCreationPolicy.Manual, new InMemoryDataProvider());
            scope.Save();
            scope.Dispose();

            scope = auditScopeFactory.Create(eventType: "eventType", target: null, extraFields: new {}, EventCreationPolicy.Manual, new InMemoryDataProvider());
            scope.Save();
            scope.Dispose();

            // Assert
            Assert.That(scope, Is.Not.Null);
            Assert.That(auditScopeFactory.OnConfiguringCalls, Is.EqualTo(5));
            Assert.That(auditScopeFactory.OnScopeCreatedCalls, Is.EqualTo(5));
        }

        [Test]
        public async Task Test_CreateAsync_OnScopeCreated_OnConfiguring_Calls()
        {
            // Arrange
            var auditScopeFactory = new MockAuditScopeFactory();

            // Act
            var scope = await auditScopeFactory.CreateAsync(new AuditScopeOptions());
            await scope.SaveAsync();
            await scope.DisposeAsync();

            scope = await auditScopeFactory.CreateAsync(cfg => cfg.EventType(""));
            await scope.SaveAsync();
            await scope.DisposeAsync();

            scope = await auditScopeFactory.CreateAsync(eventType: "eventType", target: null);
            await scope.SaveAsync();
            await scope.DisposeAsync();

            scope = await auditScopeFactory.CreateAsync(eventType: "eventType", target: null, EventCreationPolicy.Manual, new InMemoryDataProvider());
            await scope.SaveAsync();
            await scope.DisposeAsync();

            scope = await auditScopeFactory.CreateAsync(eventType: "eventType", target: null, extraFields: new { }, EventCreationPolicy.Manual, new InMemoryDataProvider());
            await scope.SaveAsync();
            await scope.DisposeAsync();

            // Assert
            Assert.That(scope, Is.Not.Null);
            Assert.That(auditScopeFactory.OnConfiguringCalls, Is.EqualTo(5));
            Assert.That(auditScopeFactory.OnScopeCreatedCalls, Is.EqualTo(5));
        }

        [Test]
        public void Test_Log_OnScopeCreated_OnConfiguring_Calls()
        {
            // Arrange
            var auditScopeFactory = new MockAuditScopeFactory();

            // Act
            auditScopeFactory.Log("eventType", extraFields: new { });
            auditScopeFactory.Log("eventType", extraFields: new { });

            // Assert
            Assert.That(auditScopeFactory.OnConfiguringCalls, Is.EqualTo(2));
            Assert.That(auditScopeFactory.OnScopeCreatedCalls, Is.EqualTo(2));
        }

        [Test]
        public async Task Test_LogAsync_OnScopeCreated_OnConfiguring_Calls()
        {
            // Arrange
            var auditScopeFactory = new MockAuditScopeFactory();

            // Act
            await auditScopeFactory.LogAsync("eventType", extraFields: new { });
            await auditScopeFactory.LogAsync("eventType", extraFields: new { });

            // Assert
            Assert.That(auditScopeFactory.OnConfiguringCalls, Is.EqualTo(2));
            Assert.That(auditScopeFactory.OnScopeCreatedCalls, Is.EqualTo(2));
        }
    }

    public class MockAuditScopeFactory : AuditScopeFactory
    {
        public int OnScopeCreatedCalls;
        public int OnConfiguringCalls;

        public override void OnScopeCreated(AuditScope auditScope)
        {
            OnScopeCreatedCalls++;
        }
        
        public override void OnConfiguring(AuditScopeOptions options)
        {
            OnConfiguringCalls++;
        }
    }
}
