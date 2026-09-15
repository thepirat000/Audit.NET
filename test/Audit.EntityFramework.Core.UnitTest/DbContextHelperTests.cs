using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;

using Audit.Core;
using Audit.Core.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class DbContextHelperTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void DbContextHelper_GetDataProvider_AuditDbContext_FromProperty()
        {
            // Arrange
            var mockContext = new Mock<AuditDbContext>();

            var helper = new DbContextHelper();
            var auditContext = new DefaultAuditContext(mockContext.Object);
            auditContext.IncludeEntityObjects = true;
            helper.SetConfig(auditContext);

            var expectedDataProvider = new NullDataProvider();

            mockContext.Setup(x => x.AuditDataProvider).Returns(expectedDataProvider);

            // Act
            var dataProvider = helper.GetDataProvider(mockContext.Object);

            // Assert
            Assert.That(dataProvider, Is.Not.Null);
            Assert.That(dataProvider, Is.SameAs(expectedDataProvider));
        }

        [Test]
        public void GetDataProvider_WhenProviderRegisteredAsIAuditDataProvider_ReturnsThatProvider()
        {
            var expectedProvider = new NullDataProvider();

            var services = new ServiceCollection();
            services.AddSingleton<IAuditDataProvider>(expectedProvider);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var mockContext = new Mock<DbContext>();

            mockContext
                .As<IInfrastructure<IServiceProvider>>()
                .Setup(inf => inf.Instance)
                .Returns(serviceProvider);

            var sut = new DbContextHelper();

            var dataProvider = sut.GetDataProvider(mockContext.Object);

            Assert.That(dataProvider, Is.SameAs(expectedProvider));
        }

        [Test]
        public void GetDataProvider_WhenProviderRegisteredAsAuditDataProvider_ReturnsThatProvider()
        {
            var expectedProvider = new NullDataProvider();

            var services = new ServiceCollection();
            services.AddSingleton<AuditDataProvider>(expectedProvider);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var mockContext = new Mock<DbContext>();

            mockContext
                .As<IInfrastructure<IServiceProvider>>()
                .Setup(inf => inf.Instance)
                .Returns(serviceProvider);

            var sut = new DbContextHelper();

            var dataProvider = sut.GetDataProvider(mockContext.Object);

            Assert.That(dataProvider, Is.SameAs(expectedProvider));
        }

        [Test]
        public void DbContextHelper_GetAuditScopeFactory_AuditDbContext_FromProperty()
        {
            // Arrange
            var mockContext = new Mock<AuditDbContext>();

            var helper = new DbContextHelper();
            var auditContext = new DefaultAuditContext(mockContext.Object);
            auditContext.IncludeEntityObjects = true;
            helper.SetConfig(auditContext);

            var expectedScopeFactory = new Mock<IAuditScopeFactory>();

            mockContext.Setup(x => x.AuditScopeFactory).Returns(expectedScopeFactory.Object);

            // Act
            var scopeFactory = helper.GetAuditScopeFactory(mockContext.Object);

            // Assert
            Assert.That(scopeFactory, Is.Not.Null);
            Assert.That(scopeFactory, Is.SameAs(expectedScopeFactory.Object));
        }

        [Test]
        public void GetDataProvider_WhenProviderRegisteredAsIAuditScopeFactory_ReturnsThatFactory()
        {
            var expectedScopeFactory = new Mock<IAuditScopeFactory>();

            var services = new ServiceCollection();
            services.AddSingleton<IAuditScopeFactory>(expectedScopeFactory.Object);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var mockContext = new Mock<DbContext>();

            mockContext
                .As<IInfrastructure<IServiceProvider>>()
                .Setup(inf => inf.Instance)
                .Returns(serviceProvider);
            
            var sut = new DbContextHelper();

            var scopeFactory = sut.GetAuditScopeFactory(mockContext.Object);

            Assert.That(scopeFactory, Is.SameAs(expectedScopeFactory.Object));
        }

        [Test]
        public void GetDataProvider_FromConfiguration_ReturnsThatFactory()
        {
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var mockContext = new Mock<DbContext>();

            mockContext
                .As<IInfrastructure<IServiceProvider>>()
                .Setup(inf => inf.Instance)
                .Returns(serviceProvider);

            var expectedScopeFactory = new Mock<IAuditScopeFactory>();
            
            Audit.Core.Configuration.AuditScopeFactory = expectedScopeFactory.Object;

            var sut = new DbContextHelper();

            var scopeFactory = sut.GetAuditScopeFactory(mockContext.Object);

            Assert.That(scopeFactory, Is.SameAs(expectedScopeFactory.Object));
        }

        [Test]
        public void GetStateName_ReturnsUnknown_ForUnhandledStates()
        {
            Assert.That(DbContextHelper.GetStateName(EntityState.Unchanged), Is.EqualTo("Unknown"));
            Assert.That(DbContextHelper.GetStateName(EntityState.Detached), Is.EqualTo("Unknown"));
        }

        [Test]
        public void GetValidationResults_ReturnsNull_WhenEntityIsValid()
        {
            var result = DbContextHelper.GetValidationResults(new ValidatedEntity { Name = "ok" });
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetValidationResults_ReturnsErrors_WhenEntityIsInvalid()
        {
            var result = DbContextHelper.GetValidationResults(new ValidatedEntity());
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GetClientConnectionId_ReturnsNull_WhenConnectionIsNull()
        {
            var helper = new DbContextHelper();
            Assert.That(helper.GetClientConnectionId(null), Is.Null);
        }

        [Test]
        public void GetClientConnectionId_ReturnsNull_WhenConnectionHasNoClientConnectionId()
        {
            var helper = new DbContextHelper();
            Assert.That(helper.GetClientConnectionId(new FakeDbConnection()), Is.Null);
        }

        [Test]
        public void TryGetClientConnectionId_ReturnsNull_WhenContextIsDisposed()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            var disposedField = typeof(DbContext).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            disposedField.SetValue(context, true);

            Assert.That(helper.TryGetClientConnectionId(context), Is.Null);
        }

        [Test]
        public void CreateAuditEvent_ReturnsNull_WhenThereAreNoModifiedEntries()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            Assert.That(helper.CreateAuditEvent(context), Is.Null);
        }

        [Test]
        public async Task CreateAuditEventAsync_ReturnsNull_WhenThereAreNoModifiedEntries()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            Assert.That(await helper.CreateAuditEventAsync(context), Is.Null);
        }

        [Test]
        public void CreateAuditEvent_WithEntryFromAnotherModel_HandlesMissingDefiningType()
        {
            using var ownerContext = new BlogsMemoryContext();
            using var otherContext = new SimpleMemoryContext();
            var auditContext = new DefaultAuditContext(otherContext);
            var helper = new DbContextHelper();
            helper.SetConfig(auditContext);

            var user = new User { Name = "cross-context" };
            ownerContext.Users.Add(user);
            var entry = ownerContext.Entry(user);

            var createEventEntry = typeof(DbContextHelper).GetMethod("CreateEventEntry", BindingFlags.NonPublic | BindingFlags.Instance);
            var eventEntry = (EventEntry)createEventEntry.Invoke(helper, new object[] { auditContext, entry });

            Assert.That(eventEntry.Name, Is.EqualTo(entry.Metadata.DisplayName()));
            Assert.That(eventEntry.ColumnValues.ContainsKey("Name"), Is.True);
            Assert.That(eventEntry.ColumnValues["Name"], Is.EqualTo("cross-context"));
        }

        [Test]
        public void CreateAuditEvent_SetsAmbientTransactionId_WhenTransactionScopeIsActive()
        {
            Audit.Core.Configuration.Setup().UseNullProvider();
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.ExcludeTransactionId = false;

            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                context.Users.Add(new User { Name = "in-tx" });
                var efEvent = helper.CreateAuditEvent(context);

                Assert.That(efEvent, Is.Not.Null);
                Assert.That(efEvent.AmbientTransactionId, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void SaveChangesGetAudit_ReturnsResultOnly_WhenAuditIsDisabled()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.AuditDisabled = true;

            var result = helper.SaveChangesGetAudit(context, () => 7);

            Assert.That(result.Result, Is.EqualTo(7));
            Assert.That(result.Entries, Is.Null);
        }

        [Test]
        public void SaveChangesGetAudit_ReturnsResultOnly_WhenThereAreNoChanges()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            var result = helper.SaveChangesGetAudit(context, () => 3);

            Assert.That(result.Result, Is.EqualTo(3));
            Assert.That(result.Entries, Is.Null);
        }

        [Test]
        public void SaveChangesGetAudit_EndsScopeWithError_WhenSaveThrows()
        {
            Audit.Core.Configuration.Setup().UseNullProvider();
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.Users.Add(new User { Name = "will-fail" });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                helper.SaveChangesGetAudit(context, () => throw new InvalidOperationException("save failed")));

            Assert.That(ex.Message, Is.EqualTo("save failed"));
        }

        [Test]
        public async Task SaveChangesGetAuditAsync_ReturnsResultOnly_WhenAuditIsDisabled()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.AuditDisabled = true;

            var result = await helper.SaveChangesGetAuditAsync(context, () => Task.FromResult(9));

            Assert.That(result.Result, Is.EqualTo(9));
            Assert.That(result.Entries, Is.Null);
        }

        [Test]
        public async Task SaveChangesGetAuditAsync_ReturnsResultOnly_WhenThereAreNoChanges()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            var result = await helper.SaveChangesGetAuditAsync(context, () => Task.FromResult(4));

            Assert.That(result.Result, Is.EqualTo(4));
            Assert.That(result.Entries, Is.Null);
        }

        [Test]
        public void SaveChangesGetAuditAsync_EndsScopeWithError_WhenSaveThrows()
        {
            Audit.Core.Configuration.Setup().UseNullProvider();
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.Users.Add(new User { Name = "async-fail" });

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await helper.SaveChangesGetAuditAsync(context, () => throw new InvalidOperationException("async save failed")));

            Assert.That(ex.Message, Is.EqualTo("async save failed"));
        }

        [Test]
        public void BeginSaveChanges_ReturnsNull_WhenAuditIsDisabled()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.AuditDisabled = true;

            Assert.That(helper.BeginSaveChanges(context), Is.Null);
        }

        [Test]
        public void BeginSaveChanges_ReturnsNull_WhenThereAreNoChanges()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            Assert.That(helper.BeginSaveChanges(context), Is.Null);
        }

        [Test]
        public async Task BeginSaveChangesAsync_ReturnsNull_WhenAuditIsDisabled()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.AuditDisabled = true;

            Assert.That(await helper.BeginSaveChangesAsync(context), Is.Null);
        }

        [Test]
        public async Task BeginSaveChangesAsync_ReturnsNull_WhenThereAreNoChanges()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);

            Assert.That(await helper.BeginSaveChangesAsync(context), Is.Null);
        }

        [Test]
        public void EndSaveChanges_DoesNothing_WhenScopeHasNoEntityFrameworkEvent()
        {
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            var scope = new Mock<IAuditScope>();
            scope.Setup(s => s.Event).Returns(new AuditEventEntityFramework { EntityFrameworkEvent = null });

            helper.EndSaveChanges(context, scope.Object, 1);
        }

        [Test]
        public async Task EndSaveChangesAsync_DoesNothing_WhenScopeHasNoEntityFrameworkEvent()
        {
            await using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            var scope = new Mock<IAuditScope>();
            scope.Setup(s => s.Event).Returns(new AuditEventEntityFramework { EntityFrameworkEvent = null });

            await helper.EndSaveChangesAsync(context, scope.Object, 1);
        }

        [Test]
        public void CreateAuditScope_CopiesExtraFields()
        {
            Audit.Core.Configuration.Setup().UseNullProvider();
            using var context = new BlogsMemoryContext();
            var helper = new DbContextHelper();
            helper.SetConfig(context);
            context.AddAuditCustomField("custom", 42);
            context.Users.Add(new User { Name = "extra-fields" });
            var efEvent = helper.CreateAuditEvent(context);

            var scope = helper.CreateAuditScope(context, efEvent);
            var auditEvent = scope.EventAs<AuditEventEntityFramework>();

            Assert.That(auditEvent.CustomFields["custom"], Is.EqualTo(42));
            scope.Dispose();
        }

        private class ValidatedEntity
        {
            [Required]
            public string Name { get; set; }
        }

        private sealed class FakeDbConnection : DbConnection
        {
            public override string ConnectionString { get; set; }
            public override string Database => "fake";
            public override string DataSource => "fake";
            public override string ServerVersion => "1";
            public override ConnectionState State => ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => null;
            protected override DbCommand CreateDbCommand() => null;
        }
    }
}
