#if EF_CORE_5
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class SaveChangesInterceptorTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>().Reset();
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_SaveChangesInterceptor_HappyPath()
        {
            var inserted = new List<AuditEventEntityFramework>();
            var replaced = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()))));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .ForEntity<InterceptContext.Department>(dc => dc.Override(d => d.Comments, "override"))
                    .IncludeEntityObjects(true));

            var guid = Guid.NewGuid().ToString();
            var dept = new InterceptContext.Department() { Name = guid, Comments = "test" };
            var options = new DbContextOptionsBuilder().AddInterceptors(new AuditSaveChangesInterceptor()).Options;
            using (var ctx = new InterceptContext(options))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
                ctx.Departments.Add(dept);
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("InterceptContext**", inserted[0].EventType);
            
            Assert.AreEqual(true, inserted[0].EntityFrameworkEvent.Success);
            Assert.AreEqual(1, inserted[0].EntityFrameworkEvent.Entries.Count);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.Entries[0].Entity.ToString().Contains(guid));
            Assert.AreEqual("Insert", inserted[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual(3, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.AreEqual(guid, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"].ToString());
            Assert.AreEqual("override", inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Comments"].ToString());
        }

        [Test]
        public async Task Test_SaveChangesInterceptor_HappyPathAsync()
        {
            var inserted = new List<AuditEventEntityFramework>();
            var replaced = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()))));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .ForEntity<InterceptContext.Department>(dc => dc.Override(d => d.Comments, "override"))
                    .IncludeEntityObjects(true));

            var guid = Guid.NewGuid().ToString();
            var dept = new InterceptContext.Department() { Name = guid, Comments = "test" };
            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditSaveChangesInterceptor()).Options))
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
                await ctx.Departments.AddAsync(dept);
                await ctx.SaveChangesAsync();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("InterceptContext**", inserted[0].EventType);

            Assert.AreEqual(true, inserted[0].EntityFrameworkEvent.Success);
            Assert.AreEqual(1, inserted[0].EntityFrameworkEvent.Entries.Count);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.Entries[0].Entity.ToString().Contains(guid));
            Assert.AreEqual("Insert", inserted[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual(3, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.AreEqual(guid, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"].ToString());
            Assert.AreEqual("override", inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Comments"].ToString());
        }

        [Test]
        public void Test_SaveChangesInterceptor_Failure()
        {
            var inserted = new List<AuditEventEntityFramework>();
            var replaced = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()))));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .IncludeEntityObjects(true));

            var dept = new InterceptContext.Department() { Id = 1, Name = "test", Comments = "test" };
            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
                ctx.Departments.Add(dept);
                ctx.SaveChanges();
            }
            Assert.AreEqual(0, inserted.Count);
            var guid = Guid.NewGuid().ToString();
            dept.Name = guid;
            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditSaveChangesInterceptor()).Options))
            {
                ctx.Departments.Add(dept);
                try
                {
                    ctx.SaveChanges();
                }
                catch (ArgumentException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("item with the same key"));
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("InterceptContext**", inserted[0].EventType);
            Assert.AreEqual(false, inserted[0].EntityFrameworkEvent.Success);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.ErrorMessage.Contains("item with the same key"));
            Assert.AreEqual(1, inserted[0].EntityFrameworkEvent.Entries.Count);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.Entries[0].Entity.ToString().Contains(guid));
            Assert.AreEqual("Insert", inserted[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual(3, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.AreEqual(guid, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"].ToString());
        }

        [Test]
        public async Task Test_SaveChangesInterceptor_FailureAsync()
        {
            var inserted = new List<AuditEventEntityFramework>();
            var replaced = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()))));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .IncludeEntityObjects(true));

            var dept = new InterceptContext.Department() { Id = 1, Name = "test", Comments = "test" };
            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
                await ctx.Departments.AddAsync(dept);
                await ctx.SaveChangesAsync();
            }
            Assert.AreEqual(0, inserted.Count);
            var guid = Guid.NewGuid().ToString();
            dept.Name = guid;
            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditSaveChangesInterceptor()).Options))
            {
                await ctx.Departments.AddAsync(dept);
                try
                {
                    await ctx.SaveChangesAsync();
                }
                catch (ArgumentException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("item with the same key"));
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("InterceptContext**", inserted[0].EventType);
            Assert.AreEqual(false, inserted[0].EntityFrameworkEvent.Success);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.ErrorMessage.Contains("item with the same key"));
            Assert.AreEqual(1, inserted[0].EntityFrameworkEvent.Entries.Count);
            Assert.IsTrue(inserted[0].EntityFrameworkEvent.Entries[0].Entity.ToString().Contains(guid));
            Assert.AreEqual("Insert", inserted[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual(3, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.AreEqual(guid, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"].ToString());
        }

        [Test]
        [TestCase(50)]
        public void Test_SaveChangesInterceptor_MultiThread(int threads)
        {
            var locker = new object();
            var inserted = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => 
                    {
                        lock (locker)
                        {
                            inserted.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()));
                        }
                    }));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<InterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .IncludeEntityObjects(true));

            using (var ctx = new InterceptContext(new DbContextOptionsBuilder().Options))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }

            Action<int> insertAction = (int index) =>
            {
                var guid = Guid.NewGuid().ToString();
                var dept = new InterceptContext.Department() { Id = index, Name = guid, Comments = "test" };
                using (var ctx = new InterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditSaveChangesInterceptor()).Options))
                {
                    ctx.Departments.Add(dept);
                    ctx.SaveChanges();
                }
            };

            var tasks = new List<Task>();
            for(int i = 0; i < threads; i++)
            {
                int a = i;
                tasks.Add(Task.Run(() => insertAction(a)));
            }
            
            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(threads, inserted.Count);
            Assert.AreEqual("InterceptContext**", inserted[0].EventType);

            Assert.AreEqual(true, inserted[0].EntityFrameworkEvent.Success);
            Assert.AreEqual(1, inserted[0].EntityFrameworkEvent.Entries.Count);
            Assert.AreEqual("Insert", inserted[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual(3, inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.AreEqual("test", inserted[0].EntityFrameworkEvent.Entries[0].ColumnValues["Comments"].ToString());
            Assert.IsTrue(Enumerable.Range(0, threads).All(i => inserted.Any(ev => ev.EntityFrameworkEvent.Entries[0].PrimaryKey["Id"].ToString() == i.ToString())));
        }
    }


    public class InterceptContext : DbContext
    {
        public DbSet<InterceptContext.Department> Departments { get; set; }
        public InterceptContext(DbContextOptions opt) : base(opt) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("InterceptDb");
            //optionsBuilder.UseSqlServer("data source=localhost;initial catalog=InterceptDb;integrated security=true;");
        }

        public class Department
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
            public string Comments { get; set; }
        }
    }

}
#endif