#if NET451
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Audit.IntegrationTest
{
    [TestFixture(Category ="EF")]
    public class EntityFrameworkTests_Net45
    {
        [Test]
        public void Test_EF_Attach()
        {
            EntityFrameworkEvent efEvent = null;
            Audit.Core.Configuration.Setup().UseDynamicProvider(x => x
                .OnInsertAndReplace(ev => {
                    efEvent = ev.GetEntityFrameworkEvent();
                }));
            using (var ctx = new MyAuditedVerboseContext())
            {
                var blog = new Blog()
                {
                    Id = 1,
                    BloggerName = "def-22"
                };
                blog.Title = "Changed-" + Guid.NewGuid();
                ctx.Blogs.Attach(blog);
                ctx.Entry(blog).State = EntityState.Modified;
                ctx.SaveChanges();
            }
            Assert.AreEqual(1, efEvent.Entries.Count);
            Assert.AreEqual("Update", efEvent.Entries[0].Action);
            Assert.IsTrue(efEvent.Entries[0].Changes.Any(ch => ch.ColumnName == "Title"));
        }

        [Test]
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

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object)
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(x => x.OnScopeCreated(sc =>
                {
                    var wcfEvent = sc.Event.GetEntityFrameworkEvent();
                    Assert.AreEqual("Blogs", wcfEvent.Database);
                }));

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
            var ev1 = (auditEvents[0] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev2 = (auditEvents[1] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev3 = (auditEvents[2] as AuditEventEntityFramework).EntityFrameworkEvent;
            Assert.NotNull(ev1.TransactionId);
            Assert.NotNull(ev2.TransactionId);
            Assert.Null(ev3.TransactionId);
            Assert.AreEqual(ev1.TransactionId, ev2.TransactionId);

            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_EF_SaveChangesSync()
        {
            Test_EF_Actions(ctx => ctx.SaveChanges());
        }

        [Test]
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

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            Database.SetInitializer<MyAuditedVerboseContext>(new CreateDatabaseIfNotExists<MyAuditedVerboseContext>());

            using (var ctx = new MyAuditedVerboseContext())
            {
                //ctx.Database.EnsureCreated();
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
                ctx.Entry(pr).State = EntityState.Deleted;

                int result = saveChangesMethod.Invoke(ctx);

                var efEvent = (auditEvent as AuditEventEntityFramework).EntityFrameworkEvent;

                Assert.AreEqual(4, result);
                Assert.AreEqual("Blogs" + "_" + ctx.GetType().Name, auditEvent.EventType);
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
        public OtherContextFromDbContext()
            : base("data source=localhost;initial catalog=Blogs;integrated security=true;")
        { }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }
    //[AuditDbContext(IncludeEntityObjects = false, Mode = AuditOptionMode.OptOut)]
    public class MyUnauditedContext : MyBaseContext
    {
        public override bool AuditDisabled
        {
            get { return true; }
            set { }
        }
    }

    [AuditDbContext(IncludeEntityObjects = true, AuditEventType = "{database}_{context}")]
    public class MyAuditedVerboseContext : MyBaseContext
    {

    }

    [AuditDbContext(IncludeEntityObjects = false, AuditEventType = "{database}_{context}")]
    public class MyAuditedContext : MyBaseContext
    {

    }

    public class MyBaseContext : AuditDbContext
    {
        public override bool AuditDisabled { get; set; }

        public MyBaseContext()
            : base("data source=localhost;initial catalog=Blogs;integrated security=true;")
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<AuditPost> AuditPosts { get; set; }
        public DbSet<AuditBlog> AuditBlogs { get; set; }
    }

    public class MyTransactionalContext : MyBaseContext
    {
        public override void OnScopeCreated(AuditScope auditScope)
        {
            Database.BeginTransaction();
        }
        public override void OnScopeSaving(AuditScope auditScope)
        {
            if (auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues.ContainsKey("BloggerName")
                && auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues["BloggerName"] == "ROLLBACK")
            {
                Database.CurrentTransaction.Rollback();
            }
            else
            {
                Database.CurrentTransaction.Commit();
            }
        }
    }

    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }

    }

    [AuditIgnore]
    public class AuditBlog : BaseEntity
    {
        public override int Id { get; set; }
        public int BlogId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Changes { get; set; }
    }
    [AuditIgnore]
    public class AuditPost : BaseEntity
    {
        public override int Id { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Changes { get; set; }
    }

    public class Blog : BaseEntity
    {
        public override int Id { get; set; }
        public string Title { get; set; }
        public string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post : BaseEntity
    {
        public override int Id { get; set; }
        public string Title { get; set; }
        public DateTime DateCreated { get; set; }
        public string Content { get; set; }
        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
#endif
