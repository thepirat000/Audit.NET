#if EF_CORE_5_OR_GREATER
using Audit.Core;
using Audit.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class DbTransactionInterceptorTests
    {
        public class DbTransactionInterceptContext : DbContext
        {
            public DbSet<Department> Departments { get; set; }
            public DbSet<User> Users { get; set; }

            public DbTransactionInterceptContext(DbContextOptions opt) : base(opt)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(
                    "data source=localhost;initial catalog=DbTransactionIntercept;integrated security=true;Encrypt=False");
                optionsBuilder.EnableSensitiveDataLogging();

            }

            public class User
            {
                [Key]
                [DatabaseGenerated(DatabaseGeneratedOption.None)]
                public int Id { get; set; }
                public string UserName { get; set; }
            }

            public class Department
            {
                [Key]
                [DatabaseGenerated(DatabaseGeneratedOption.None)]
                public int Id { get; set; }

                public string Name { get; set; }
                public string Comments { get; set; }

                [Required]
                public int UserId { get; set; }
                public User User { get; set; }
            }
        }

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>().Reset();
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_DbTransactionInterceptor_HappyPath()
        {
            var inserted = new List<AuditEventTransactionEntityFramework>();
            var replaced = new List<AuditEventTransactionEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(
                        ev => inserted.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }

            var interceptor = new AuditTransactionInterceptor() {AuditEventType = "{database}:{transaction}"};
            int id = new Random().Next();
            int uid = new Random().Next();
            var user = new DbTransactionInterceptContext.User() {Id = uid, UserName = "test"};
            var dept = new DbTransactionInterceptContext.Department()
            {
                Id = id,
                Comments = "Test",
                Name = "Name",
                User = user
            };
            
            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder()
                       .AddInterceptors(interceptor).Options))
            {
                ctx.Departments.Add(dept);
                ctx.SaveChanges();

                var deptLoaded = ctx.Departments.FirstOrDefault();
                Assert.IsNotNull(deptLoaded);
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.IsNotNull(inserted[0].TransactionEvent.TransactionId);
            Assert.AreEqual("Start", inserted[0].TransactionEvent.Action);
            Assert.IsNotNull(inserted[0].TransactionEvent.ConnectionId);
            Assert.IsNotNull(inserted[0].TransactionEvent.DbConnectionId);
            Assert.IsNull(inserted[0].TransactionEvent.ErrorMessage);
            Assert.IsFalse(inserted[0].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[0].TransactionEvent.Success);
            Assert.AreEqual($"DbTransactionIntercept:{inserted[0].TransactionEvent.TransactionId}",
                inserted[0].EventType);

            Assert.AreEqual("Commit", inserted[1].TransactionEvent.Action);
            Assert.AreEqual($"DbTransactionIntercept:{inserted[1].TransactionEvent.TransactionId}",
                inserted[1].EventType);
            Assert.IsNotNull(inserted[1].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[1].TransactionEvent.ErrorMessage);
            Assert.IsFalse(inserted[1].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[1].TransactionEvent.Success);

            Assert.AreEqual(inserted[0].TransactionEvent.ConnectionId, inserted[1].TransactionEvent.ConnectionId);
            Assert.AreEqual(inserted[0].TransactionEvent.DbConnectionId, inserted[1].TransactionEvent.DbConnectionId);
            Assert.AreEqual(inserted[0].TransactionEvent.TransactionId, inserted[1].TransactionEvent.TransactionId);
        }

        [Test]
        public async Task Test_DbTransactionInterceptor_HappyPathAsync()
        {
            var inserted = new List<AuditEventTransactionEntityFramework>();
            var replaced = new List<AuditEventTransactionEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(
                        ev => inserted.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
            }

            var interceptor = new AuditTransactionInterceptor() { AuditEventType = "{database}:{transaction}" };
            int id = new Random().Next();
            int uid = new Random().Next();
            var user = new DbTransactionInterceptContext.User() { Id = uid, UserName = "test" };
            var dept = new DbTransactionInterceptContext.Department()
            {
                Id = id,
                Comments = "Test",
                Name = "Name",
                User = user
            };

            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder()
                       .AddInterceptors(interceptor).Options))
            {
                ctx.Departments.Add(dept);
                await ctx.SaveChangesAsync();

                var deptLoaded = await ctx.Departments.FirstOrDefaultAsync();
                Assert.IsNotNull(deptLoaded);
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.IsNotNull(inserted[0].TransactionEvent.TransactionId);
            Assert.AreEqual("Start", inserted[0].TransactionEvent.Action);
            Assert.IsNotNull(inserted[0].TransactionEvent.ConnectionId);
            Assert.IsNotNull(inserted[0].TransactionEvent.DbConnectionId);
            Assert.IsNull(inserted[0].TransactionEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[0].TransactionEvent.Success);
            Assert.AreEqual($"DbTransactionIntercept:{inserted[0].TransactionEvent.TransactionId}",
                inserted[0].EventType);

            Assert.AreEqual("Commit", inserted[1].TransactionEvent.Action);
            Assert.AreEqual($"DbTransactionIntercept:{inserted[1].TransactionEvent.TransactionId}",
                inserted[1].EventType);
            Assert.IsNotNull(inserted[1].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[1].TransactionEvent.ErrorMessage);
            Assert.IsTrue(inserted[1].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[1].TransactionEvent.Success);

            Assert.AreEqual(inserted[0].TransactionEvent.ConnectionId, inserted[1].TransactionEvent.ConnectionId);
            Assert.AreEqual(inserted[0].TransactionEvent.DbConnectionId, inserted[1].TransactionEvent.DbConnectionId);
            Assert.AreEqual(inserted[0].TransactionEvent.TransactionId, inserted[1].TransactionEvent.TransactionId);
        }

        [Test]
        public void Test_DbTransactionInterceptor_Rollback()
        {
            var inserted = new List<AuditEventTransactionEntityFramework>();
            var replaced = new List<AuditEventTransactionEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(
                        ev => inserted.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }

            int id = new Random().Next();

            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder()
                       .AddInterceptors(new AuditTransactionInterceptor()).Options))
            {
                using (var tran = ctx.Database.BeginTransaction())
                {
                    var user = new DbTransactionInterceptContext.User() {Id = 1, UserName = "test"};
                    ctx.Users.Add(user);
                    ctx.Departments.Add(new DbTransactionInterceptContext.Department()
                        {Id = id, Comments = "Test", Name = "Name", User = user});

                    ctx.SaveChanges();

                    tran.Rollback();
                }
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual($"DbTransactionIntercept:{inserted[0].TransactionEvent.TransactionId}", inserted[0].EventType);
            Assert.IsNotNull(inserted[0].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[0].TransactionEvent.ErrorMessage);
            Assert.IsFalse(inserted[0].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[0].TransactionEvent.Success);
            Assert.AreEqual("Start", inserted[0].TransactionEvent.Action);

            Assert.IsNotNull(inserted[1].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[1].TransactionEvent.ErrorMessage);
            Assert.IsFalse(inserted[1].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[1].TransactionEvent.Success);
            Assert.AreEqual("Rollback", inserted[1].TransactionEvent.Action);
        }

        [Test]
        public async Task Test_DbTransactionInterceptor_RollbackAsync()
        {
            var inserted = new List<AuditEventTransactionEntityFramework>();
            var replaced = new List<AuditEventTransactionEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(
                        ev => inserted.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventTransactionEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
            }

            int id = new Random().Next();

            using (var ctx = new DbTransactionInterceptContext(new DbContextOptionsBuilder()
                       .AddInterceptors(new AuditTransactionInterceptor()).Options))
            {
                using (var tran = await ctx.Database.BeginTransactionAsync())
                {
                    var user = new DbTransactionInterceptContext.User() { Id = 1, UserName = "test" };
                    ctx.Users.Add(user);
                    ctx.Departments.Add(new DbTransactionInterceptContext.Department()
                    { Id = id, Comments = "Test", Name = "Name", User = user });

                    await ctx.SaveChangesAsync();

                    await tran.RollbackAsync();
                }
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual($"DbTransactionIntercept:{inserted[0].TransactionEvent.TransactionId}", inserted[0].EventType);
            Assert.IsNotNull(inserted[0].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[0].TransactionEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[0].TransactionEvent.Success);
            Assert.AreEqual("Start", inserted[0].TransactionEvent.Action);

            Assert.IsNotNull(inserted[1].TransactionEvent.ConnectionId);
            Assert.IsNull(inserted[1].TransactionEvent.ErrorMessage);
            Assert.IsTrue(inserted[1].TransactionEvent.IsAsync);
            Assert.IsTrue(inserted[1].TransactionEvent.Success);
            Assert.AreEqual("Rollback", inserted[1].TransactionEvent.Action);
        }


    }

}
#endif