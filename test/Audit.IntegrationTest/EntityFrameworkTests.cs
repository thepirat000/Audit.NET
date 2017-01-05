using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
#if NETCOREAPP1_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Data.Entity;
#endif

namespace Audit.IntegrationTest
{
    [TestFixture(Category ="EF")]
    public class EntityFrameworkTests
    {
        [Test]
        public void Test_EF_IncludeIgnoreFilters()
        {
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(p => p
                    .OnInsert(ev =>
                    {
                        logs.Add(ev);
                    }));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptOut()
                        .IgnoreAny(t => t.Name.StartsWith("Blo"));

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = "post1-test",
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(1, logs[0].GetEntityFrameworkEvent().Entries.Count);
            Assert.AreEqual("Posts", logs[0].GetEntityFrameworkEvent().Entries[0].Table);

            logs.Clear();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptIn()
                        .IncludeAny(t => t.Name.StartsWith("Blog"));

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = "post1-test",
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(1, logs[0].GetEntityFrameworkEvent().Entries.Count);
            Assert.AreEqual("Blogs", logs[0].GetEntityFrameworkEvent().Entries[0].Table);
#if NET451
            Assert.IsTrue(logs[0].Environment.CallingMethodName.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
#endif
        }


        [Test]
        public void Test_EF_SaveOnSameContext_Transaction()
        {
            var b1Title = Guid.NewGuid().ToString().Substring(1, 10);
            var p1Title = Guid.NewGuid().ToString().Substring(1, 10);
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(p => p
                    .OnInsert(ev =>
                    {
                        logs.Add(ev);
                    }))
                 .WithCreationPolicy(EventCreationPolicy.Manual);

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = b1Title,
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = p1Title,
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            { 
                var p = ctx.Posts.FirstOrDefault(x => x.Title == p1Title);
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == b1Title);
                Assert.NotNull(p);
                Assert.NotNull(b);
                ctx.Blogs.Remove(b);
                ctx.Posts.Remove(p);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "ROLLBACK",
                    Title = "blog1-test"
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            {
                var p = ctx.Posts.FirstOrDefault(x => x.Title == p1Title);
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == b1Title);
                Assert.Null(p);
                Assert.Null(b);
            }

            Assert.AreEqual(3, logs.Count);
#if NET451
            Assert.IsTrue(logs[0].Environment.CallingMethodName.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
#endif
        }

#if NETCOREAPP1_0
        private IDbContextTransaction GetCurrentTran(DbContext context)
        {
            var dbtxmgr = context.GetInfrastructure().GetService<IDbContextTransactionManager>();
            var relcon = dbtxmgr as IRelationalConnection;
            return relcon.CurrentTransaction;
        }
#else
        private DbContextTransaction GetCurrentTran(DbContext context)
        {
            return context.Database.CurrentTransaction;
        }
#endif
    }
}
