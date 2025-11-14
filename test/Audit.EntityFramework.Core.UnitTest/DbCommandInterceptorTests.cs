#if EF_CORE_5_OR_GREATER
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework.Interceptors;
using Audit.IntegrationTest;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using NUnit.Framework;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
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
            Audit.Core.Configuration.Reset();
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

                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(2));
            Assert.That(replaced.Count, Is.EqualTo(0));

            Assert.That(inserted[0].CommandEvent.CommandText.Contains("SELECT"), Is.True);
            Assert.That(inserted[0].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[0].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage, Is.Null);
            Assert.IsFalse(inserted[0].CommandEvent.IsAsync);
            Assert.That(inserted[0].CommandEvent.Parameters, Is.Empty.Or.Null);
            Assert.That(inserted[0].CommandEvent.Result, Is.Null);
            Assert.That(inserted[0].CommandEvent.Success, Is.True);
            Assert.That(inserted[0].EventType, Is.EqualTo("DbCommandInterceptContext:DbCommandIntercept:ExecuteReader"));

            Assert.That(inserted[1].EventType, Is.EqualTo("DbCommandInterceptContext:DbCommandIntercept:ExecuteNonQuery"));
            Assert.That(inserted[1].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(inserted[1].CommandEvent.CommandText.Contains("INSERT INTO DEPARTMENTS"), Is.True);
            Assert.That(inserted[1].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[1].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[1].CommandEvent.ErrorMessage, Is.Null);
            Assert.IsFalse(inserted[1].CommandEvent.IsAsync);
            Assert.That(inserted[1].CommandEvent.Parameters.Count, Is.EqualTo(2));
            Assert.That(inserted[1].CommandEvent.Parameters.Any(p => p.Value.ToString() == "comments"), Is.True);
            Assert.That(inserted[1].CommandEvent.Parameters.Any(p => p.Value.ToString() == id.ToString()), Is.True);
            Assert.That(inserted[1].CommandEvent.Result.ToString(), Is.EqualTo("1"));
            Assert.That(inserted[1].CommandEvent.Success, Is.True);

            Assert.That(inserted[1].CommandEvent.ConnectionId, Is.EqualTo(inserted[0].CommandEvent.ConnectionId));
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

                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(2));
            Assert.That(replaced.Count, Is.EqualTo(0));

            Assert.That(inserted[0].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteReader));
            Assert.That(inserted[0].CommandEvent.CommandText.Contains("SELECT"), Is.True);
            Assert.That(inserted[0].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[0].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage, Is.Null);
            Assert.That(inserted[0].CommandEvent.IsAsync, Is.True);
            Assert.That(inserted[0].CommandEvent.Parameters, Is.Empty.Or.Null);
            Assert.That(inserted[0].CommandEvent.Result, Is.Null);
            Assert.That(inserted[0].CommandEvent.Success, Is.True);
            Assert.That(inserted[0].EventType, Is.EqualTo("DbCommandInterceptContext:DbCommandIntercept:ExecuteReader"));

            Assert.That(inserted[1].EventType, Is.EqualTo("DbCommandInterceptContext:DbCommandIntercept:ExecuteNonQuery"));
            Assert.That(inserted[1].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(inserted[1].CommandEvent.CommandText.Contains("INSERT INTO DEPARTMENTS"), Is.True);
            Assert.That(inserted[1].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[1].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[1].CommandEvent.ErrorMessage, Is.Null);
            Assert.That(inserted[1].CommandEvent.IsAsync, Is.True);
            Assert.That(inserted[1].CommandEvent.Parameters.Count, Is.EqualTo(1));
            Assert.That(inserted[1].CommandEvent.Parameters.First().Value.ToString(), Is.EqualTo("comments"));
            Assert.That(inserted[1].CommandEvent.Result.ToString(), Is.EqualTo("1"));
            Assert.That(inserted[1].CommandEvent.Success, Is.True);

            Assert.That(inserted[1].CommandEvent.ConnectionId, Is.EqualTo(inserted[0].CommandEvent.ConnectionId));
        }

        [Test]
        public void Test_DbCommandInterceptor_Failure()
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


            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));


            Assert.That(inserted[0].EventType, Is.EqualTo(DbCommandMethod.ExecuteNonQuery.ToString()));
            Assert.That(inserted[0].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(inserted[0].CommandEvent.CommandText.Contains("SELECT 1/0"), Is.True);
            Assert.That(inserted[0].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[0].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage.Contains("Divide by zero"), Is.True);
            Assert.IsFalse(inserted[0].CommandEvent.IsAsync);
            Assert.That(inserted[0].CommandEvent.Result, Is.Null);
            Assert.IsFalse(inserted[0].CommandEvent.Success);
        }

        [Test]
        public async Task Test_DbCommandInterceptor_FailureAsync()
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].EventType, Is.EqualTo(DbCommandMethod.ExecuteNonQuery.ToString()));
            Assert.That(inserted[0].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(inserted[0].CommandEvent.CommandText.Contains("SELECT 1/0"), Is.True);
            Assert.That(inserted[0].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(inserted[0].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage, Is.Not.Null);
            Assert.That(inserted[0].CommandEvent.ErrorMessage.Contains("Divide by zero"), Is.True);
            Assert.That(inserted[0].CommandEvent.IsAsync, Is.True);
            Assert.That(inserted[0].CommandEvent.Result, Is.Null);
            Assert.IsFalse(inserted[0].CommandEvent.Success);
            Assert.That(inserted[0].CommandEvent.Database, Is.EqualTo("DbCommandIntercept"));
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(inserted[0].Parameters, Is.Not.Null);
            Assert.That(inserted[0].Parameters.Any(), Is.True);
            Assert.That(inserted[0].Parameters.First().Value, Is.EqualTo(newId));
            Assert.That(inserted[0].Result, Is.Not.Null);
            var resultList = inserted[0].Result as Dictionary<string, List<Dictionary<string, object>>>;
            Assert.That(resultList.Count, Is.EqualTo(1));
            Assert.That((int)resultList.Values.First()[0]["Id"], Is.EqualTo(newId));
            Assert.That(resultList.Values.First()[0]["Comments"], Is.EqualTo("Comment"));
            Assert.That(resultList.Values.First()[0]["Name"], Is.EqualTo("Test"));
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(inserted[0].Parameters, Is.Not.Null);
            Assert.That(inserted[0].Parameters.Any(), Is.True);
            Assert.That(inserted[0].Parameters.First().Value, Is.EqualTo(newId));
            Assert.That(inserted[0].Result, Is.Not.Null);
            var resultList = inserted[0].Result as Dictionary<string, List<Dictionary<string, object>>>;
            Assert.That(resultList.Count, Is.EqualTo(1));
            Assert.That((int)resultList.Values.First()[0]["Id"], Is.EqualTo(newId));
            Assert.That(resultList.Values.First()[0]["Comments"], Is.EqualTo("Comment"));
            Assert.That(resultList.Values.First()[0]["Name"], Is.EqualTo("Test"));
        }

#if EF_CORE_6_OR_GREATER
        [TestCase(CommandSource.LinqQuery)]
        [TestCase(CommandSource.SaveChanges)]
        public void Test_DbCommandInterceptor_IncludeReaderPredicate(CommandSource sourceToInclude)
        {
            var commandEvents = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => commandEvents.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            int newId = new Random().Next();

            var interceptor = new AuditCommandInterceptor()
            {
                LogParameterValues = true,
                IncludeReaderEventsPredicate = c => c.CommandSource == sourceToInclude,
                ExcludeScalarEvents = false
            };

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                // Intercepted SaveChanges
                var dept = new DbCommandInterceptContext.Department() { Id = newId, Name = "Test", Comments = "Comment" };
                ctx.Departments.Add(dept);
                ctx.SaveChanges();
                
                // intercepted LinqQuery
                dept = ctx.Departments.FirstOrDefault(d => d.Id == newId);

                Assert.That(dept, Is.Not.Null);
            }

            Assert.That(commandEvents.Count, Is.EqualTo(1));
            Assert.That(commandEvents[0].Parameters, Is.Not.Null);
            Assert.That(commandEvents[0].Parameters.Any(), Is.True);
            Assert.That(commandEvents[0].Parameters.First().Value, Is.EqualTo(newId));
            Assert.That(commandEvents[0].Result, Is.Null);
            Assert.That(commandEvents[0].CommandSource, Is.EqualTo(sourceToInclude));
        }

        [TestCase(CommandSource.LinqQuery)]
        [TestCase(CommandSource.SaveChanges)]
        public async Task Test_DbCommandInterceptor_IncludeReaderPredicateAsync(CommandSource sourceToInclude)
        {
            var commandEvents = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => commandEvents.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            int newId = new Random().Next();

            var interceptor = new AuditCommandInterceptor()
            {
                LogParameterValues = true,
                IncludeReaderEventsPredicate = c => c.CommandSource == sourceToInclude,
                ExcludeScalarEvents = false
            };

            using (var ctx = new DbCommandInterceptContext(new DbContextOptionsBuilder().AddInterceptors(interceptor).Options))
            {
                // Intercepted SaveChanges
                var dept = new DbCommandInterceptContext.Department() { Id = newId, Name = "Test", Comments = "Comment" };
                await ctx.Departments.AddAsync(dept);
                await ctx.SaveChangesAsync();

                // intercepted LinqQuery
                dept = await ctx.Departments.FirstOrDefaultAsync(d => d.Id == newId);

                Assert.That(dept, Is.Not.Null);
            }

            Assert.That(commandEvents.Count, Is.EqualTo(1));
            Assert.That(commandEvents[0].Parameters, Is.Not.Null);
            Assert.That(commandEvents[0].Parameters.Any(), Is.True);
            Assert.That(commandEvents[0].Parameters.First().Value, Is.EqualTo(newId));
            Assert.That(commandEvents[0].Result, Is.Null);
            Assert.That(commandEvents[0].CommandSource, Is.EqualTo(sourceToInclude));
        }

        [Test]
        public void Test_DbCommandInterceptor_ExecuteScalar()
        {
            var commandEvents = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => commandEvents.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var interceptor = new AuditCommandInterceptor()
            {
                LogParameterValues = true,
                ExcludeScalarEvents = false
            };

            using (var ctx = new DbCommandInterceptContext(interceptor))
            {
                var history = ctx.GetService<IHistoryRepository>();

                // This should trigger two events: NonQueryExecuting and ScalarExecuting
                history.Exists();
            }

            Assert.That(commandEvents.Count, Is.EqualTo(2));
            Assert.That(commandEvents[0].Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(commandEvents[1].Method, Is.EqualTo(DbCommandMethod.ExecuteScalar));
        }

        [Test]
        public async Task Test_DbCommandInterceptor_ExecuteScalarAsync()
        {
            var commandEvents = new List<CommandEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => commandEvents.Add(ev.GetCommandEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var interceptor = new AuditCommandInterceptor()
            {
                LogParameterValues = true,
                ExcludeScalarEvents = false
            };

            await using (var ctx = new DbCommandInterceptContext(interceptor))
            {
                var history = ctx.GetService<IHistoryRepository>();

                // This should trigger two events: NonQueryExecuting and ScalarExecuting
                await history.ExistsAsync();
            }

            Assert.That(commandEvents.Count, Is.EqualTo(2));
            Assert.That(commandEvents[0].Method, Is.EqualTo(DbCommandMethod.ExecuteNonQuery));
            Assert.That(commandEvents[1].Method, Is.EqualTo(DbCommandMethod.ExecuteScalar));
        }
#endif

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

            Assert.That(inserted.Count, Is.EqualTo(1));

            Assert.That((inserted[0].Result as ICollection)?.Count, Is.EqualTo(2)); // Two result sets
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

            Assert.That(insertedCommands.Count, Is.EqualTo(1));
            Assert.That(insertedSavechanges.Count, Is.EqualTo(1));
            Assert.That(insertedCommands[0].EventType, Is.EqualTo(DbCommandMethod.ExecuteReader.ToString()));
            Assert.That(insertedCommands[0].CommandEvent.Method, Is.EqualTo(DbCommandMethod.ExecuteReader));
            Assert.That(insertedCommands[0].CommandEvent.CommandText.Contains("INSERT INTO"), Is.True);
            Assert.That(insertedCommands[0].CommandEvent.CommandType, Is.EqualTo(CommandType.Text));
            Assert.That(insertedCommands[0].CommandEvent.ConnectionId, Is.Not.Null);
            Assert.That(insertedCommands[0].CommandEvent.ContextId, Is.Not.Null);
            Assert.That(insertedCommands[0].CommandEvent.ErrorMessage, Is.Null);
            Assert.IsFalse(insertedCommands[0].CommandEvent.IsAsync);
            Assert.That(insertedCommands[0].CommandEvent.Parameters, Is.Not.Null);
            Assert.That(insertedCommands[0].CommandEvent.Result, Is.Null);
            Assert.That(insertedCommands[0].CommandEvent.Success, Is.True);
            Assert.That(insertedSavechanges[0].EntityFrameworkEvent.ConnectionId, Is.EqualTo(insertedCommands[0].CommandEvent.ConnectionId));
            Assert.That(insertedCommands[0].CommandEvent.Database, Is.EqualTo("DbCommandIntercept"));
            Assert.That(insertedSavechanges[0].EntityFrameworkEvent.Database, Is.EqualTo(insertedCommands[0].CommandEvent.Database));
            Assert.That(insertedSavechanges[0].EntityFrameworkEvent.ContextId, Is.EqualTo(insertedCommands[0].CommandEvent.ContextId));
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(inserted[0].CommandEvent.Parameters, Is.Null);
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(1));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].EventType, Is.EqualTo("DbCommandInterceptContext_InheritingFromAuditDbContext | DbCommandIntercept | ExecuteNonQuery"));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].EventType, Is.EqualTo("DbCommandInterceptContext_InheritingFromAuditDbContext | DbCommandIntercept | ExecuteNonQuery"));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_DbCommandInterceptor_AuditDisabledFromGlobalConfig()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            Audit.Core.Configuration.AuditDisabled = true;

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: false,
                       eventType: null,
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = ctx.Database.ExecuteSqlRaw("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.That(result, Is.EqualTo(1));
            }

            Audit.Core.Configuration.AuditDisabled = false;
            
            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Test_DbCommandInterceptor_AuditDisabledFromGlobalConfigAsync()
        {
            var inserted = new List<AuditEventCommandEntityFramework>();
            var replaced = new List<AuditEventCommandEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(d => d
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson())))
                    .OnReplace((eventId, ev) =>
                        replaced.Add(AuditEvent.FromJson<AuditEventCommandEntityFramework>(ev.ToJson()))));

            int id = _rnd.Next();

            Audit.Core.Configuration.AuditDisabled = true;

            var optionsWithInterceptor = new DbContextOptionsBuilder()
                .AddInterceptors(new AuditCommandInterceptor())
                .Options;
            using (var ctx = new DbCommandInterceptContext_InheritingFromAuditDbContext(
                       opt: optionsWithInterceptor,
                       dataProvider: null,
                       auditDisabled: false,
                       eventType: null,
                       customFieldValue: null))
            {
                //NonQueryExecuting
                var result = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO DEPARTMENTS (Id, Name, Comments) VALUES (" + id + ", 'test', {0})", "comments");
                Assert.That(result, Is.EqualTo(1));
            }

            Audit.Core.Configuration.AuditDisabled = false;

            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].CustomFields.ContainsKey("customField"), Is.True);
            Assert.That(inserted[0].CustomFields["customField"].ToString(), Is.EqualTo(id.ToString()));
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
                Assert.That(result, Is.EqualTo(1));
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].CustomFields.ContainsKey("customField"), Is.True);
            Assert.That(inserted[0].CustomFields["customField"].ToString(), Is.EqualTo(id.ToString()));
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
            Assert.That(result, Is.EqualTo(1));


            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].CustomFields.ContainsKey("customField"), Is.True);
            Assert.That(inserted[0].CustomFields["customField"].ToString(), Is.EqualTo(id.ToString()));
            Assert.That(dbContext.ScopeCreatedCommands.Count, Is.EqualTo(1));
            Assert.That(dbContext.ScopeSavingCommands.Count, Is.EqualTo(1));
            Assert.That(dbContext.ScopeSavedCommands.Count, Is.EqualTo(1));
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
            Assert.That(result, Is.EqualTo(1));


            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].CustomFields.ContainsKey("customField"), Is.True);
            Assert.That(inserted[0].CustomFields["customField"].ToString(), Is.EqualTo(id.ToString()));
            Assert.That(dbContext.ScopeCreatedCommands.Count, Is.EqualTo(1));
            Assert.That(dbContext.ScopeSavingCommands.Count, Is.EqualTo(1));
            Assert.That(dbContext.ScopeSavedCommands.Count, Is.EqualTo(1));
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

        public DbCommandInterceptContext_InheritingFromAuditDbContext(DbContextOptions opt, IAuditDataProvider dataProvider, bool auditDisabled, string eventType, string customFieldValue) : base(opt)
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
        private readonly IInterceptor _interceptor;

        public static string CnnString = TestHelper.GetConnectionString("DbCommandIntercept");

        public DbSet<Department> Departments { get; set; }

        public DbCommandInterceptContext(DbContextOptions opt) : base(opt) { }

        public DbCommandInterceptContext(IInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
            if (_interceptor != null)
            {
                optionsBuilder.AddInterceptors(_interceptor);
            }
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