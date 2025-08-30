#if NET7_0_OR_GREATER
using System;
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
    public class DbContextDataProviderGenericTests
    {
        [Test]
        public void Test_DbContextDataProvider_Configuration_DbContextBuilder()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider<MyDbContext, AuditLog>(c => c
                .DbContext(ctx)
                .Mapper((ev, auditLog) =>
                {
                    auditLog.JsonData = ev.ToJson();
                    auditLog.CreatedDate = DateTime.Now;
                })
                .DisposeDbContext());

            var log = new AuditLog();

            Assert.That(ctx, Is.EqualTo(dp.DbContextBuilder(null)));
            Assert.That(dp.DisposeDbContext, Is.True);
            Assert.That(dp.Mapper, Is.Not.Null);
            dp.Mapper.Invoke(new AuditEvent(), log);
            Assert.That(log.JsonData, Is.Not.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_DbContext()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider<MyDbContext, AuditLog>(c => c
                .DbContextBuilder(ev => ctx)
                .Mapper((ev, auditLog) =>
                {
                    auditLog.JsonData = ev.ToJson();
                    auditLog.CreatedDate = DateTime.Now;
                })
                .DisposeDbContext());

            var log = new AuditLog();

            Assert.That(ctx, Is.EqualTo(dp.DbContextBuilder(null)));
            Assert.That(dp.DisposeDbContext, Is.True);
            Assert.That(dp.Mapper, Is.Not.Null);
            dp.Mapper.Invoke(new AuditEvent(), log);
            Assert.That(log.JsonData, Is.Not.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_DbContext_NonGeneric()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider(c => c
                .DbContext(ctx)
                .DisposeDbContext());

            Assert.That(ctx, Is.EqualTo(dp.DbContextBuilder(null)));
            Assert.That(dp.DisposeDbContext, Is.True);
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_UseDbContextOptions()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>().Options;
            var dp = new DbContextDataProvider<MyDbContext, AuditLog>(c => c
                .UseDbContextOptions(options)
                .Mapper((ev, auditLog) =>
                {
                    auditLog.JsonData = ev.ToJson();
                    auditLog.CreatedDate = DateTime.Now;
                })
                .DisposeDbContext());

            var log = new AuditLog();

            Assert.That(dp.DbContextOptions.GetDefault(), Is.SameAs(options));
            Assert.That(dp.DisposeDbContext, Is.True);
            Assert.That(dp.Mapper, Is.Not.Null);
            dp.Mapper.Invoke(new AuditEvent(), log);
            Assert.That(log.JsonData, Is.Not.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_UseDbContextOptions_Event_NonGeneric()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>().Options;
            var dp = new DbContextDataProvider(c => c
                .UseDbContextOptions(ev => options)
                .DisposeDbContext());

            Assert.That(dp.DbContextOptions.GetDefault(), Is.SameAs(options));
            Assert.That(dp.DisposeDbContext, Is.True);
        }

        [Test]
        public void Test_DbContextDataProvider_Configuration_UseDbContextOptions_NonGeneric()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>().Options;
            var dp = new DbContextDataProvider(c => c
                .UseDbContextOptions(options)
                .DisposeDbContext());

            Assert.That(dp.DbContextOptions.GetDefault(), Is.SameAs(options));
            Assert.That(dp.DisposeDbContext, Is.True);
        }

        [Test]
        public void Test_DbContextDataProvider_GetDbContext_Builder()
        {
            var ctx = new MyDbContext();

            var dp = new DbContextDataProvider<MyDbContext, AuditLog>()
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
            var dp = new DbContextDataProvider<MyDbContext, AuditLog>()
            {
                DbContextOptions = new DbContextOptionsBuilder<MyDbContext>().UseInMemoryDatabase("test").Options,
                DisposeDbContext = true
            };

            var dbContext = dp.GetDbContext(new AuditEvent());

            Assert.That(dbContext.DbName, Is.Null);
        }

        [Test]
        public void Test_DbContextDataProvider_GetDbContext_ParameterlessConstructor()
        {
            var dp = new DbContextDataProvider<MyDbContext, AuditLog>()
            {
                DisposeDbContext = true
            };

            var dbContext = dp.GetDbContext(new AuditEvent());

            Assert.That(dbContext.DbName, Is.EqualTo("Default"));
        }
        
        [Test]
        public void Test_DbContextDataProvider_InsertOnStartReplaceOnEnd()
        {
            // Arrange
            var dbContext = new MyDbContext(Guid.NewGuid().ToString());

            var dp = new DbContextDataProvider<MyDbContext, AuditLog>()
            {
                DbContextBuilder = ev => dbContext,
                Mapper = (auditEvent, auditLog) =>
                {
                    auditLog.JsonData = auditEvent.ToJson();
                    if (auditEvent.GetScope().EventId == null)
                    {
                        auditLog.CreatedDate = DateTime.Now;
                    }

                    auditLog.UpdatedDate = DateTime.Now;
                },
                DisposeDbContext = false
            };

            AuditEvent eventBefore;

            // Act
            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp, CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd }))
            {
                eventBefore = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogs.First().JsonData);
                scope.SetCustomField("Test", "1");
            }

            var eventAfter = JsonSerializer.Deserialize<AuditEvent>(dbContext.AuditLogs.First().JsonData);

            // Assert
            Assert.That(eventAfter.StartDate, Is.EqualTo(eventBefore.StartDate));
            Assert.That(eventBefore.EndDate, Is.Null);
            Assert.That(eventAfter.EndDate, Is.Not.Null);
            Assert.That(eventBefore.CustomFields, Is.Null.Or.Empty);
            Assert.That(eventAfter.CustomFields, Does.ContainKey("Test"));
        }

        [Test]
        public async Task Test_DbContextDataProvider_InsertOnStartReplaceOnEndAsync()
        {
            // Arrange
            var dbContext = new MyDbContext(Guid.NewGuid().ToString());

            var dp = new DbContextDataProvider<MyDbContext, AuditLog>()
            {
                DbContextBuilder = ev => dbContext,
                Mapper = (auditEvent, auditLog) =>
                {
                    auditLog.JsonData = auditEvent.ToJson();
                    if (auditEvent.GetScope().EventId == null)
                    {
                        auditLog.CreatedDate = DateTime.Now;
                    }

                    auditLog.UpdatedDate = DateTime.Now;
                },
                DisposeDbContext = false
            };

            AuditEvent eventBefore;

            // Act
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = dp, CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd }))
            {
                eventBefore = JsonSerializer.Deserialize<AuditEvent>((await dbContext.AuditLogs.FirstAsync()).JsonData);
                scope.SetCustomField("Test", "1");
            }

            var eventAfter = JsonSerializer.Deserialize<AuditEvent>((await dbContext.AuditLogs.FirstAsync()).JsonData);

            // Assert
            Assert.That(eventAfter.StartDate, Is.EqualTo(eventBefore.StartDate));
            Assert.That(eventBefore.EndDate, Is.Null);
            Assert.That(eventAfter.EndDate, Is.Not.Null);
            Assert.That(eventBefore.CustomFields, Is.Null.Or.Empty);
            Assert.That(eventAfter.CustomFields, Does.ContainKey("Test"));
        }

        // Support classes

        public class MyDbContext : DbContext
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

            public DbSet<AuditLog> AuditLogs { get; set; }

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
    }
}
#endif

