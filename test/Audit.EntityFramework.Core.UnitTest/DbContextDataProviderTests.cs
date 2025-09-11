#if NET7_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Audit.Core;
using Audit.EntityFramework.Providers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class DbContextDataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_DbContextBuilder()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider(c => c
                .DbContextBuilder(ev => ctx)
                .EntityBuilder(ev => new AuditLog
                {
                    JsonData = ev.ToJson(),
                    CreatedDate = DateTime.Now
                })
                .DisposeDbContext());

            Assert.That(ctx, Is.EqualTo(dp.DbContextBuilder(null)));
            Assert.That(dp.DisposeDbContext, Is.True);
            Assert.That(dp.EntityBuilder, Is.Not.Null);
            var entities = dp.EntityBuilder.Invoke(new AuditEvent()) as List<object>;
            Assert.That(entities, Is.Not.Null);
            Assert.That(entities, Has.Count.EqualTo(1));
            Assert.That(entities[0], Is.TypeOf<AuditLog>());
            Assert.That((entities[0] as AuditLog)?.JsonData, Is.Not.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_GetDbContext_Builder()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider()
            {
                DbContextBuilder = _ => ctx,
                DisposeDbContext = true
            };

            var dbContext = dp.GetDbContext(new AuditEvent());

            Assert.That(dbContext, Is.EqualTo(ctx));
        }

        [Test]
        public void Test_DbContextDataProvider_GetDbContext_Options()
        {
            var dp = new DbContextDataProvider()
            {
                DbContextOptions = new DbContextOptionsBuilder<MyDbContext>().UseInMemoryDatabase("test").Options,
                DisposeDbContext = true
            };

            var dbContext = dp.GetDbContext(new AuditEvent());

            Assert.That(dbContext as MyDbContext, Is.Not.Null);
            Assert.That((dbContext as MyDbContext).DbName, Is.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_NoDbContext_NoOptions_Error()
        {
            var dp = new DbContextDataProvider()
            {
                DbContextOptions = new Setting<DbContextOptions>((DbContextOptions)null),
                DbContextBuilder = null,
                DisposeDbContext = true
            };

            Assert.Throws<InvalidOperationException>(() =>
            {
                var dbContext = dp.GetDbContext(new AuditEvent());
            });
        }

        [Test]
        public void Test_DbContextDataProvider_InsertOnEnd()
        {
            // Arrange
            var dbContext = new MyDbContext(Guid.NewGuid().ToString());

            var dp = new DbContextDataProvider(config => config
                .DbContextBuilder(ev => dbContext)
                .EntityBuilder(auditEvent =>
                {
                    var auditLog = new AuditLog
                    {
                        JsonData = auditEvent.ToJson(),
                        UpdatedDate = DateTime.Now
                    };
                    if (auditEvent.GetScope().EventId == null)
                    {
                        auditLog.CreatedDate = DateTime.Now;
                    }
                    return auditLog;
                })
                .DisposeDbContext(false)
            );

            // Act
            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp, CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
                scope.SetCustomField("Test", "1");
            }

            var auditEvent = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogs.First().JsonData);

            // Assert
            Assert.That(auditEvent.EndDate, Is.Not.Null);
            Assert.That(auditEvent.CustomFields, Does.ContainKey("Test"));
        }

        [Test]
        public async Task Test_DbContextDataProvider_InsertOnEndAsync()
        {
            // Arrange
            var dbContext = new MyDbContext(Guid.NewGuid().ToString());

            Audit.Core.Configuration.Setup()
                .UseDbContext(config => config
                    .DbContextBuilder(ev => dbContext)
                    .EntityBuilder(auditEvent =>
                    {
                        var auditLog = new AuditLog
                        {
                            JsonData = auditEvent.ToJson(),
                            UpdatedDate = DateTime.Now
                        };
                        if (auditEvent.GetScope().EventId == null)
                        {
                            auditLog.CreatedDate = DateTime.Now;
                        }
                        return auditLog;
                    })
                    .DisposeDbContext(false));

            // Act
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
                scope.SetCustomField("Test", "1");
            }

            var auditEvent = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogs.First().JsonData);

            // Assert
            Assert.That(auditEvent.EndDate, Is.Not.Null);
            Assert.That(auditEvent.CustomFields, Does.ContainKey("Test"));
        }
        
        [Test]
        public void Test_DbContextDataProvider_SkipEntities_DisposeDbContext()
        {
            // Arrange
            var dbId = Guid.NewGuid().ToString();
            var dp = new DbContextDataProvider(config => config
                .DbContextBuilder(ev => new MyDbContext(dbId))
                .EntityBuilder(auditEvent =>
                {
                    if (auditEvent.EventType == "Skip")
                    {
                        return null;
                    }

                    return new AuditLog() { JsonData = "Test" };
                })
                .DisposeDbContext(true)
            );

            // Act
            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp, EventType = "Skip", CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
            }

            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp, EventType = "DoNotSkip", CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
            }

            // Assert
            var dbContext = new MyDbContext(dbId);
            var jsonData = dbContext.AuditLogs.Single().JsonData;

            Assert.That(jsonData, Is.EqualTo("Test"));
        }

        [Test]
        public async Task Test_DbContextDataProvider_SkipEntities_DisposeDbContextAsync()
        {
            // Arrange
            var dbId = Guid.NewGuid().ToString();
            var dp = new DbContextDataProvider(config => config
                .DbContextBuilder(ev => new MyDbContext(dbId))
                .EntityBuilder(auditEvent =>
                {
                    if (auditEvent.EventType == "Skip")
                    {
                        return null;
                    }

                    return new AuditLog() { JsonData = "Test" };
                })
                .DisposeDbContext(true)
            );

            // Act
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = dp, EventType = "Skip", CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
            }

            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = dp, EventType = "DoNotSkip", CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
            }

            var dbContext = new MyDbContext(dbId);
            var jsonData = dbContext.AuditLogs.Single().JsonData;

            Assert.That(jsonData, Is.EqualTo("Test"));
        }

        [Test]
        public async Task Test_DbContextDataProvider_MultipleEntities()
        {
            // Arrange
            var dbContext = new MyDbContext(Guid.NewGuid().ToString());

            Audit.Core.Configuration.Setup()
                .UseDbContext(config => config
                    .DbContextBuilder(ev => dbContext)
                    .EntityBuilder(auditEvent =>
                    {
                        var auditLog = new AuditLog
                        {
                            JsonData = auditEvent.ToJson(),
                            UpdatedDate = DateTime.Now
                        };

                        var auditLogSpecial = new AuditLogSpecial()
                        {
                            JsonData = auditEvent.ToJson()
                        };

                        return new List<object>() { auditLog, auditLogSpecial };
                    })
                    .DisposeDbContext(false));

            // Act
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
                scope.SetCustomField("Test", "1");
            }

            var auditEvent = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogs.First().JsonData);
            var auditEventSpecial = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogsSpecial.First().JsonData);

            // Assert
            Assert.That(auditEvent.EndDate, Is.Not.Null);
            Assert.That(auditEvent.CustomFields, Does.ContainKey("Test"));
            Assert.That(auditEventSpecial.EndDate, Is.Not.Null);
            Assert.That(auditEventSpecial.CustomFields, Does.ContainKey("Test"));
        }

        // Support classes

        internal class MyDbContext : DbContext
        {
            internal readonly string DbName;

            public MyDbContext()
            {
                DbName = "Default";
            }

            public MyDbContext(string dbName)
            {
                DbName = dbName;
            }

            public MyDbContext(DbContextOptions options) : base(options)
            {
                DbName = null;
            }

            public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
            public DbSet<AuditLogSpecial> AuditLogsSpecial => Set<AuditLogSpecial>();

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (DbName != null)
                {
                    optionsBuilder.UseInMemoryDatabase(DbName);
                }
            }
        }

        public class AuditLog
        {
            public int Id { get; set; }
            public string JsonData { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
        }

        public class AuditLogSpecial
        {
            public int Id { get; set; }
            public string JsonData { get; set; }
        }

    }
}
#endif