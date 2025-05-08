using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
