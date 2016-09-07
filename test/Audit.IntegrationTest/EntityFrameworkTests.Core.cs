#if NETCOREAPP1_0
using Audit.Core;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Audit.IntegrationTest
{
    public class EntityFrameworkTests_Core
    {
        [Fact]
        public void Test_EF_Transaction()
        {
            var provider = new Mock<AuditDataProvider>();
            var auditEvents = new List<AuditEvent>();
            provider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                auditEvents.Add(ev);
                return Guid.NewGuid();
            });
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            AuditConfiguration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 1", DateCreated = DateTime.Now, Title = "other title 1" });
                    ctx.SaveChanges();
                    ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 2", DateCreated = DateTime.Now, Title = "other title 2" });
                    ctx.SaveChanges();
                    transaction.Rollback();
                }
                ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 3", DateCreated = DateTime.Now, Title = "other title 3" });
                ctx.SaveChanges();
            }
            var ev1 = (auditEvents[0].CustomFields["EntityFrameworkEvent"] as EntityFrameworkEvent);
            var ev2 = (auditEvents[1].CustomFields["EntityFrameworkEvent"] as EntityFrameworkEvent);
            var ev3 = (auditEvents[2].CustomFields["EntityFrameworkEvent"] as EntityFrameworkEvent);
            Assert.NotNull(ev1.TransactionId);
            Assert.NotNull(ev2.TransactionId);
            Assert.Null(ev3.TransactionId);
            Assert.Equal(ev1.TransactionId, ev2.TransactionId);
        }

        [Fact]
        public void Test_EF_Direct()
        {
            using (var ctx = new OtherContextFromDbContext())
            {
                ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content", DateCreated = DateTime.Now, Title = "other title" });
                var efEvent = AuditDbContext.CreateAuditEvent(ctx, true, AuditOptionMode.OptOut);
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && (e.Entity as Post)?.Title == "other title"));
            }
        }

        [Fact]
        public void Test_EF_SaveChangesSync()
        {
            Test_EF_Actions(ctx => ctx.SaveChanges());
        }

        [Fact]
        public void Test_EF_SaveChangesAsync()
        {
            Test_EF_Actions(ctx => ctx.SaveChangesAsync().Result);
        }

        public void Test_EF_Actions(Func<DbContext, int> saveChangesMethod)
        {
            var provider = new Mock<AuditDataProvider>();
            AuditEvent auditEvent = null;
            provider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                auditEvent = ev;
                return Guid.NewGuid();
            });
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            AuditConfiguration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlCommand(@"
delete from Posts
delete from Blogs
SET IDENTITY_INSERT Blogs ON 
insert into Blogs (id, title, bloggername) values (1, 'abc', 'def')
insert into Blogs (id, title, bloggername) values (2, 'ghi', 'jkl')
SET IDENTITY_INSERT Blogs OFF
SET IDENTITY_INSERT Posts ON
insert into Posts (id, title, datecreated, content, blogid) values (1, 'my post 1', GETDATE(), 'this is an example 123', 1)
insert into Posts (id, title, datecreated, content, blogid) values (2, 'my post 2', GETDATE(), 'this is an example 456', 1)
insert into Posts (id, title, datecreated, content, blogid) values (3, 'my post 3', GETDATE(), 'this is an example 789', 1)
insert into Posts (id, title, datecreated, content, blogid) values (4, 'my post 4', GETDATE(), 'this is an example 987', 2)
insert into Posts (id, title, datecreated, content, blogid) values (5, 'my post 5', GETDATE(), 'this is an example 000', 2)
SET IDENTITY_INSERT Posts OFF
                    ");

                var postsblog1 = ctx.Blogs.Include(x => x.Posts)
                    .FirstOrDefault(x => x.Id == 1);
                postsblog1.BloggerName += "-22";

                ctx.Posts.Add(new Post() { BlogId = 1, Content = "content", DateCreated = DateTime.Now, Title = "title" });

                var ch1 = ctx.Posts.FirstOrDefault(x => x.Id == 1);
                ch1.Content += "-code";

                var pr = ctx.Posts.FirstOrDefault(x => x.Id == 5);
                ctx.Remove(pr);

                int result = saveChangesMethod.Invoke(ctx);

                var efEvent = auditEvent.CustomFields["EntityFrameworkEvent"] as EntityFrameworkEvent;

                Assert.Equal(4, result);
                Assert.Equal("Blogs" + "_" + ctx.GetType().Name, auditEvent.EventType);
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && e.ColumnValues["Title"].Equals("title") && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Update" && (e.Entity as Blog)?.Id == 1 && e.Changes[0].ColumnName == "BloggerName"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && (e.Entity as Post)?.Id == 5));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && e.ColumnValues["Id"].Equals(5) && (e.Entity as Post)?.Id == 5));
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);

            }
        }
    }

    public class OtherContextFromDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // from dbcontext
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Blogs;integrated security=true;");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }
    
    public class MyUnauditedContext : MyBaseContext
    {
        protected override bool AuditDisabled
        {
            get { return true; }
            set { }
        }
    }

    [AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = true, AuditEventType = "{database}_{context}")]
    public class MyAuditedVerboseContext : MyBaseContext
    {

    }

    [AuditDbContext(IncludeEntityObjects = false)]
    public class MyAuditedContext : MyBaseContext
    {

    }

    public class MyBaseContext : AuditDbContext
    {
        protected override bool AuditDisabled { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Blogs;integrated security=true;");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }

    public class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post
    {
        public int Id { get; set; }
        [MaxLength(20)]
        public string Title { get; set; }
        public DateTime DateCreated { get; set; }
        public string Content { get; set; }
        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
#endif
