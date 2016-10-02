using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
using Xunit;

namespace Audit.IntegrationTest
{
    [Collection("EF")]
    public class EntityFrameworkTests
    {

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

        [Fact]
        public void Test_EF_SaveOnSameContext_Transaction()
        {
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

            using (var ctx = new MyTransactionalContext())
            { 
                var p = ctx.Posts.FirstOrDefault(x => x.Title == "post1-test");
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == "blog1-test");
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
                var p = ctx.Posts.FirstOrDefault(x => x.Title == "post1-test");
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == "blog1-test");
                Assert.Null(p);
                Assert.Null(b);
            }

            Assert.Equal(3, logs.Count);
        }
    }
}
