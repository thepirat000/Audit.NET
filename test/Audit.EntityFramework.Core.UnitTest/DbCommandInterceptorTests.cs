#if EF_CORE_3 || EF_CORE_5
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
    public class DbCommandInterceptorTests
    {
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
        public void Test_DbCommandInterceptor_HappyPath()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) => replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
            var interceptor = new AuditCommandInterceptor() { AuditEventType = "{database}:{method}" };
            int id = new Random().Next();
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                //ReaderExecuting
                var depts = ctx.Departments.Where(d => d.Comments != null).ToList();

                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES ({0}, 'test', {1})", id, "comments");

                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.IsTrue(inserted[0].CommandEvent.CommandText.Contains("SELECT"));
            Assert.AreEqual(CommandType.Text, inserted[0].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[0].CommandEvent.ConnectionId);
            Assert.IsNull(inserted[0].CommandEvent.ErrorMessage);
            Assert.IsFalse(inserted[0].CommandEvent.IsAsync);
            Assert.IsNull(inserted[0].CommandEvent.Parameters);
            Assert.IsNull(inserted[0].CommandEvent.Result);
            Assert.IsTrue(inserted[0].CommandEvent.Success);
            Assert.AreEqual("DbCommandIntercept:ExecuteReader", inserted[0].EventType);

            Assert.AreEqual("DbCommandIntercept:ExecuteNonQuery", inserted[1].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery, inserted[1].CommandEvent.Method);
            Assert.IsTrue(inserted[1].CommandEvent.CommandText.Contains("INSERT INTO DEPARTMENTS"));
            Assert.AreEqual(CommandType.Text, inserted[1].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[1].CommandEvent.ConnectionId);
            Assert.IsNull(inserted[1].CommandEvent.ErrorMessage);
            Assert.IsFalse(inserted[1].CommandEvent.IsAsync);
            Assert.AreEqual(2, inserted[1].CommandEvent.Parameters.Count);
            Assert.IsTrue(inserted[1].CommandEvent.Parameters.Any(p => p.Value.ToString() == "comments"));
            Assert.IsTrue(inserted[1].CommandEvent.Parameters.Any(p => p.Value.ToString() == id.ToString()));
            Assert.AreEqual("1", inserted[1].CommandEvent.Result.ToString()); 
            Assert.IsTrue(inserted[1].CommandEvent.Success);

            Assert.AreEqual(inserted[0].CommandEvent.ConnectionId, inserted[1].CommandEvent.ConnectionId);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_HappyPathAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) => replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
            }
            int id = new Random().Next();
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditCommandInterceptor()).Options))
            {
                //ReaderExecuting
                var depts = await ctx.Departments.Where(d => d.Comments != null).ToListAsync();

                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");

                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual(DbCommandMethod.ExecuteReader.ToString(), inserted[0].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteReader, inserted[0].CommandEvent.Method);
            Assert.IsTrue(inserted[0].CommandEvent.CommandText.Contains("SELECT"));
            Assert.AreEqual(CommandType.Text, inserted[0].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[0].CommandEvent.ConnectionId);
            Assert.IsNull(inserted[0].CommandEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].CommandEvent.IsAsync);
            Assert.IsNull(inserted[0].CommandEvent.Parameters);
            Assert.IsNull(inserted[0].CommandEvent.Result);
            Assert.IsTrue(inserted[0].CommandEvent.Success);

            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery.ToString(), inserted[1].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery, inserted[1].CommandEvent.Method);
            Assert.IsTrue(inserted[1].CommandEvent.CommandText.Contains("INSERT INTO DEPARTMENTS"));
            Assert.AreEqual(CommandType.Text, inserted[1].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[1].CommandEvent.ConnectionId);
            Assert.IsNull(inserted[1].CommandEvent.ErrorMessage);
            Assert.IsTrue(inserted[1].CommandEvent.IsAsync);
            Assert.AreEqual(1, inserted[1].CommandEvent.Parameters.Count);
            Assert.AreEqual("comments", inserted[1].CommandEvent.Parameters.First().Value.ToString());
            Assert.AreEqual("1", inserted[1].CommandEvent.Result.ToString());
            Assert.IsTrue(inserted[1].CommandEvent.Success);

            Assert.AreEqual(inserted[0].CommandEvent.ConnectionId, inserted[1].CommandEvent.ConnectionId);
        }

        [Test]
        public void Test_DbCommandInterceptor_Failed()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditCommandInterceptor()).Options))
            {
                try
                {
                    var result = ctx.Database.ExecuteSqlRaw("SELECT 1/0");
                }
                catch
                {
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery.ToString(), inserted[0].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery, inserted[0].CommandEvent.Method);
            Assert.IsTrue(inserted[0].CommandEvent.CommandText.Contains("SELECT 1/0"));
            Assert.AreEqual(CommandType.Text, inserted[0].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[0].CommandEvent.ConnectionId);
            Assert.IsNotNull(inserted[0].CommandEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].CommandEvent.ErrorMessage.Contains("Divide by zero"));
            Assert.IsFalse(inserted[0].CommandEvent.IsAsync);
            Assert.IsNull(inserted[0].CommandEvent.Result);
            Assert.IsFalse(inserted[0].CommandEvent.Success);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_FailedAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((id, ev) => replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .AuditEventType("{context}**")
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditCommandInterceptor()).Options))
            {
                try
                {
                    var result = await ctx.Database.ExecuteSqlRawAsync("SELECT 1/0");
                }
                catch (Exception ex)
                {
                    var e = ex;
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery.ToString(), inserted[0].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteNonQuery, inserted[0].CommandEvent.Method);
            Assert.IsTrue(inserted[0].CommandEvent.CommandText.Contains("SELECT 1/0"));
            Assert.AreEqual(CommandType.Text, inserted[0].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[0].CommandEvent.ConnectionId);
            Assert.IsNotNull(inserted[0].CommandEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].CommandEvent.ErrorMessage.Contains("Divide by zero"));
            Assert.IsTrue(inserted[0].CommandEvent.IsAsync);
            Assert.IsNull(inserted[0].CommandEvent.Result);
            Assert.IsFalse(inserted[0].CommandEvent.Success);
            Assert.AreEqual("DbCommandIntercept", inserted[0].CommandEvent.Database);
        }

#if EF_CORE_5
        [Test]
        public void Test_DbCommandInterceptor_CombineSaveChanges()
        {
            var insertedCommands = new List<AuditEventCommandEntityFramework>();
            var insertedSavechanges = new List<AuditEventEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => 
                    {
                        if (ev is AuditEventEntityFramework)
                        {
                            insertedSavechanges.Add(AuditEvent.FromJson<AuditEventEntityFramework>(ev.ToJson()));
                        }
                        else
                        {
                            insertedCommands.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()));
                        }
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd); 

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureCreated();
            }

            var id = new Random().Next();
            var guid = Guid.NewGuid().ToString();
            var dept = new DbCommandInterceptContext.Department() { Id = id, Name = guid, Comments = "test" };
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor(), new AuditSaveChangesInterceptor()).Options))
            {
                //ReaderExecuting
                ctx.Departments.Add(dept);
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, insertedCommands.Count);
            Assert.AreEqual(1, insertedSavechanges.Count);

            Assert.AreEqual(DbCommandMethod.ExecuteReader.ToString(), insertedCommands[0].EventType);
            Assert.AreEqual(DbCommandMethod.ExecuteReader, insertedCommands[0].CommandEvent.Method);
            Assert.IsTrue(insertedCommands[0].CommandEvent.CommandText.Contains("INSERT INTO"));
            Assert.AreEqual(CommandType.Text, insertedCommands[0].CommandEvent.CommandType);
            Assert.IsNotNull(insertedCommands[0].CommandEvent.ConnectionId);
            Assert.IsNotNull(insertedCommands[0].CommandEvent.ContextId);
            Assert.IsNull(insertedCommands[0].CommandEvent.ErrorMessage);
            Assert.IsFalse(insertedCommands[0].CommandEvent.IsAsync);
            Assert.IsNotNull(insertedCommands[0].CommandEvent.Parameters);
            Assert.IsNull(insertedCommands[0].CommandEvent.Result);
            Assert.IsTrue(insertedCommands[0].CommandEvent.Success);
            Assert.AreEqual(insertedCommands[0].CommandEvent.ConnectionId, insertedSavechanges[0].EntityFrameworkEvent.ConnectionId);
            Assert.AreEqual("DbCommandIntercept", insertedCommands[0].CommandEvent.Database);
            Assert.AreEqual(insertedCommands[0].CommandEvent.Database, insertedSavechanges[0].EntityFrameworkEvent.Database);
            Assert.AreEqual(insertedCommands[0].CommandEvent.ContextId, insertedSavechanges[0].EntityFrameworkEvent.ContextId);
        }
#endif
        
        [Test]
        public void Test_DbCommandInterceptor_IgnoreParams()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                ctx.Database.EnsureCreated();
            }
            var interceptor = new AuditCommandInterceptor() { LogParameterValues = false };
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("SELECT {0}", "test");
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.IsNull(inserted[0].CommandEvent.Parameters);
        }

        [Test]
        public void Test_DbCommandInterceptor_CreationPolicy()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DbCommandInterceptContext>(_ => _
                    .IncludeEntityObjects(true));
            int id = new Random().Next();
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor()).Options))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
        }
    }

    public class DbCommandInterceptContext : DbContext
    {
        public DbSet<Department> Departments { get; set; }
        public DbCommandInterceptContext(DbContextOptions opt) : base(opt) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=DbCommandIntercept;integrated security=true;");
            optionsBuilder.EnableSensitiveDataLogging();
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