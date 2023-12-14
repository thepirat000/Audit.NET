#if EF_CORE_5_OR_GREATER
using Audit.Core;
using Audit.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core.Providers;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class DbCommandInterceptorTests
    {
        private static Random _rnd = new Random();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
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
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
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

            var interceptor = new AuditCommandInterceptor() { AuditEventType = "{context}:{database}:{method}" };
            int id = _rnd.Next();
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                //ReaderExecuting
                var depts = ctx.Departments.Where(d => d.Comments != null).FirstOrDefault();

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
            Assert.AreEqual("DbCommandInterceptContext:DbCommandIntercept:ExecuteReader", inserted[0].EventType);

            Assert.AreEqual("DbCommandInterceptContext:DbCommandIntercept:ExecuteNonQuery", inserted[1].EventType);
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

            int id = _rnd.Next();
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(new AuditCommandInterceptor() { AuditEventType = "{context}:{database}:{method}" }).Options))
            {
                //ReaderExecuting
                var depts = await ctx.Departments.Where(d => d.Comments != null).ToListAsync();

                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");

                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);

            Assert.AreEqual(DbCommandMethod.ExecuteReader, inserted[0].CommandEvent.Method);
            Assert.IsTrue(inserted[0].CommandEvent.CommandText.Contains("SELECT"));
            Assert.AreEqual(CommandType.Text, inserted[0].CommandEvent.CommandType);
            Assert.IsNotNull(inserted[0].CommandEvent.ConnectionId);
            Assert.IsNull(inserted[0].CommandEvent.ErrorMessage);
            Assert.IsTrue(inserted[0].CommandEvent.IsAsync);
            Assert.IsNull(inserted[0].CommandEvent.Parameters);
            Assert.IsNull(inserted[0].CommandEvent.Result);
            Assert.IsTrue(inserted[0].CommandEvent.Success);
            Assert.AreEqual("DbCommandInterceptContext:DbCommandIntercept:ExecuteReader", inserted[0].EventType);

            Assert.AreEqual("DbCommandInterceptContext:DbCommandIntercept:ExecuteNonQuery", inserted[1].EventType);
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

        [Test]
        public void Test_DbCommandInterceptor_IncludeReaderResult()
        {
            var inserted = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            int newId = 24;
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                var dept = new DbCommandInterceptContext.Department() { Id = newId, Name = "Test", Comments = "Comment" };
                ctx.Departments.Add(dept);
                ctx.SaveChanges();
            }

            var interceptor = new AuditCommandInterceptor() { LogParameterValues = true, IncludeReaderResults = true };
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                var dept = ctx.Departments.FirstOrDefault(d => d.Id == newId);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.IsNotNull(inserted[0].Parameters);
            Assert.IsTrue(inserted[0].Parameters.Any());
            Assert.AreEqual(newId, inserted[0].Parameters.First().Value);
            Assert.IsNotNull(inserted[0].Result);
            var resultList = inserted[0].Result as Dictionary<string, List<Dictionary<string, object>>>;
            Assert.AreEqual(1, resultList.Count);
            Assert.AreEqual(newId, (int)resultList.Values.First()[0]["Id"]);
            Assert.AreEqual("Comment", resultList.Values.First()[0]["Comments"]);
            Assert.AreEqual("Test", resultList.Values.First()[0]["Name"]);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_IncludeReaderResultAsync()
        {
            var inserted = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            int newId = 23;
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().Options))
            {
                // not intercepted
                var dept = new DbCommandInterceptContext.Department() { Id = newId, Name = "Test", Comments = "Comment" };
                await ctx.Departments.AddAsync(dept);
                await ctx.SaveChangesAsync();
            }

            var interceptor = new AuditCommandInterceptor() { LogParameterValues = true, IncludeReaderResults = true };
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                var dept = ctx.Departments.FirstOrDefault(d => d.Id == newId);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.IsNotNull(inserted[0].Parameters);
            Assert.IsTrue(inserted[0].Parameters.Any());
            Assert.AreEqual(newId, inserted[0].Parameters.First().Value);
            Assert.IsNotNull(inserted[0].Result);
            var resultList = inserted[0].Result as Dictionary<string, List<Dictionary<string, object>>>;
            Assert.AreEqual(1, resultList.Count);
            Assert.AreEqual(newId, (int)resultList.Values.First()[0]["Id"]);
            Assert.AreEqual("Comment", resultList.Values.First()[0]["Comments"]);
            Assert.AreEqual("Test", resultList.Values.First()[0]["Name"]);
        }

#if EF_CORE_7_OR_GREATER
        [Test]
        public async Task Test_DbCommandInterceptor_IncludeReaderResult_MultipleResultSets_EfCore7_Async()
        {
            var inserted = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var interceptor = new AuditCommandInterceptor() { LogParameterValues = true, IncludeReaderResults = true };
            
            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                var dept = new DbCommandInterceptContext.Department()
                {
                    Name = "Dept1",
                    Address = new DbCommandInterceptContext.Address()
                    {
                        Text = "Addr1"
                    }
                };
                await ctx.Departments.AddAsync(dept);
                await ctx.SaveChangesAsync();
                inserted.Clear();

                dept.Name = "DeptUpdated";
                dept.Address.Text = "AddrUpdated";
                await ctx.SaveChangesAsync();
            }
            
            Assert.AreEqual(1, inserted.Count);
            
            Assert.AreEqual(2, (inserted[0].Result as ICollection)?.Count); // Two result sets
        }
#endif

#if EF_CORE_5_OR_GREATER
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

            var id = _rnd.Next();
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

            int id = _rnd.Next();
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

        [Test]
        public void Test_DbCommandInterceptor_DataProviderFromAuditDbContext()
        {
            Audit.Core.Configuration.Setup()
                .UseNullProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            var dynamicDataProvider = new DynamicDataProvider(d => d
                .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                .OnReplace((eventId, ev) =>
                    replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));
            
            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: dynamicDataProvider,
                       auditDisabled: false,
                       eventType: "{context} | {database} | {method}",
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("DbCommandInterceptContext_InheritingFromAuditDbContext | DbCommandIntercept | ExecuteNonQuery", inserted[0].EventType);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_DataProviderFromAuditDbContextAsync()
        {
            Audit.Core.Configuration.Setup()
                .UseNullProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();
            var dynamicDataProvider = new DynamicDataProvider(d => d
                .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                .OnReplace((eventId, ev) =>
                    replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: dynamicDataProvider,
                       auditDisabled: false,
                       eventType: "{context} | {database} | {method}",
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("DbCommandInterceptContext_InheritingFromAuditDbContext | DbCommandIntercept | ExecuteNonQuery", inserted[0].EventType);
        }

        [Test]
        public void Test_DbCommandInterceptor_AuditDisabledFromAuditDbContext()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                .OnReplace((eventId, ev) =>
                    replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: true,
                       eventType: null,
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(0, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_AuditDisabledFromAuditDbContextAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: true,
                       eventType: null,
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(0, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
        }

        [Test]
        public void Test_DbCommandInterceptor_CustomFieldFromAuditDbContext()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: false,
                       eventType: null,
                       customFieldValue: id.ToString()))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsTrue(inserted[0].CustomFields.ContainsKey("customField"));
            Assert.AreEqual(id.ToString(), inserted[0].CustomFields["customField"].ToString());
        }

        [Test]
        public async Task Test_DbCommandInterceptor_CustomFieldFromAuditDbContextAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: false,
                       eventType: null,
                       customFieldValue: id.ToString()))
            {
                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.AreEqual(1, result);
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsTrue(inserted[0].CustomFields.ContainsKey("customField"));
            Assert.AreEqual(id.ToString(), inserted[0].CustomFields["customField"].ToString());
        }

        [Test]
        public void Test_DbCommandInterceptor_OnScopeXFromAuditDbContext()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            
            var dbContext = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                opt: optionsWithInterceptor,
                dataProvider: null,
                auditDisabled: false,
                eventType: null,
                customFieldValue: id.ToString());

            //NonQueryExecuting
            var result = dbContext.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
            Assert.AreEqual(1, result);
            

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsTrue(inserted[0].CustomFields.ContainsKey("customField"));
            Assert.AreEqual(id.ToString(), inserted[0].CustomFields["customField"].ToString());
            Assert.AreEqual(1, dbContext.ScopeCreatedCommands.Count);
            Assert.AreEqual(1, dbContext.ScopeSavingCommands.Count);
            Assert.AreEqual(1, dbContext.ScopeSavedCommands.Count);
            dbContext.Dispose();
        }

        [Test]
        public async Task Test_DbCommandInterceptor_OnScopeXFromAuditDbContextAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;

            var dbContext = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                opt: optionsWithInterceptor,
                dataProvider: null,
                auditDisabled: false,
                eventType: null,
                customFieldValue: id.ToString());

            //NonQueryExecuting
            var result = await dbContext.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
            Assert.AreEqual(1, result);


            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsTrue(inserted[0].CustomFields.ContainsKey("customField"));
            Assert.AreEqual(id.ToString(), inserted[0].CustomFields["customField"].ToString());
            Assert.AreEqual(1, dbContext.ScopeCreatedCommands.Count);
            Assert.AreEqual(1, dbContext.ScopeSavingCommands.Count);
            Assert.AreEqual(1, dbContext.ScopeSavedCommands.Count);
            await dbContext.DisposeAsync();
        }
    }

    public class DbCommandInterceptContext_InheritingFromAuditDbContext : AuditDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("DbCommandIntercept");

        public DbSet<Department> Departments { get; set; }

        public List<CommandEvent> ScopeCreatedCommands { get; set; } = new List<CommandEvent>();
        public List<CommandEvent> ScopeSavingCommands { get; set; } = new List<CommandEvent>();
        public List<CommandEvent> ScopeSavedCommands { get; set; } = new List<CommandEvent>();

        public DbCommandInterceptContext_InheritingFromAuditDbContext(DbContextOptions opt, AuditDataProvider dataProvider, bool auditDisabled, string eventType, string customFieldValue) : base(opt)
        {
            base.AuditDataProvider = dataProvider;
            base.AuditDisabled = auditDisabled;
            base.AuditEventType = eventType;
            if (customFieldValue != null)
            {
                base.AddAuditCustomField("customField", customFieldValue);
            }
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        public override void OnScopeCreated(IAuditScope auditScope)
        {
            ScopeCreatedCommands.Add(auditScope.GetCommandEntityFrameworkEvent());
        }

        public override void OnScopeSaving(IAuditScope auditScope)
        {
            ScopeSavingCommands.Add(auditScope.GetCommandEntityFrameworkEvent());
        }

        public override void OnScopeSaved(IAuditScope auditScope)
        {
            ScopeSavedCommands.Add(auditScope.GetCommandEntityFrameworkEvent());
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

    public class DbCommandInterceptContext : DbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("DbCommandIntercept");

        public DbSet<Department> Departments { get; set; }

        public DbCommandInterceptContext(DbContextOptions opt) : base(opt) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        public class Department
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
            public string Comments { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Text { get; set; }
        }
    }
}
#endif